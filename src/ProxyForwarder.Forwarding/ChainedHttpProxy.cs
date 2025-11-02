using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyForwarder.Forwarding;

/// <summary>
/// Minimal explicit-proxy that chains to an upstream HTTP proxy (no TLS MITM, no local DNS resolution for destinations).
/// </summary>
public sealed class ChainedHttpProxy : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly string _upHost;
    private readonly int _upPort;
    private readonly string? _proxyAuth;
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;

    public ChainedHttpProxy(IPAddress bind, int localPort, string upstreamHost, int upstreamPort, string? user = null, string? pass = null)
    {
        _listener = new TcpListener(bind, localPort);
        _upHost = upstreamHost;
        _upPort = upstreamPort;

        if (!string.IsNullOrEmpty(user))
        {
            var b64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass ?? ""}"));
            _proxyAuth = $"Proxy-Authorization: Basic {b64}\r\n";
        }
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            System.Diagnostics.Debug.WriteLine($"[ChainedHttpProxy] Listening on 127.0.0.1:{_listener.LocalEndpoint}");
            _acceptLoop = Task.Run(AcceptLoopAsync);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChainedHttpProxy] Start error: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { }
        if (_acceptLoop is not null) await _acceptLoop;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
                System.Diagnostics.Debug.WriteLine($"[ChainedHttpProxy] Client connected");
                _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChainedHttpProxy] Accept error: {ex.Message}");
                client?.Dispose();
            }
        }
    }

    private static async Task<byte[]?> ReadHeadersAsync(NetworkStream s, CancellationToken ct, int maxHeaderBytes = 64 * 1024)
    {
        var buf = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var ms = new MemoryStream();
            int match = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                s.ReadTimeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
                int n = await s.ReadAsync(buf.AsMemory(0, buf.Length), ct);
                if (n == 0) return null;
                ms.Write(buf, 0, n);

                // Find \r\n\r\n
                for (int i = Math.Max(0, (int)ms.Length - n - 4); i < ms.Length; i++)
                {
                    byte b = (ms.GetBuffer())[i];
                    match = (match switch
                    {
                        0 => b == (byte)'\r' ? 1 : 0,
                        1 => b == (byte)'\n' ? 2 : 0,
                        2 => b == (byte)'\r' ? 3 : 0,
                        3 => b == (byte)'\n' ? 4 : 0,
                        _ => 0
                    });
                    if (match == 4)
                    {
                        return ms.ToArray();
                    }
                }
                if (ms.Length > maxHeaderBytes)
                    throw new IOException("Header too large");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private static (string method, string target, string version) ParseRequestLine(ReadOnlySpan<byte> head)
    {
        int sp1 = head.IndexOf((byte)' ');
        int sp2 = head.Slice(sp1 + 1).IndexOf((byte)' ') + sp1 + 1;
        var method = Encoding.ASCII.GetString(head[..sp1]);
        var target = Encoding.ASCII.GetString(head[(sp1 + 1)..sp2]);
        var version = Encoding.ASCII.GetString(head[(sp2 + 1)..head.IndexOf(new byte[] { (byte)'\r', (byte)'\n' })]);
        return (method, target, version);
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var c = client;
        try
        {
            c.NoDelay = true;
            using var cs = c.GetStream();

        byte[]? headerBytes = await ReadHeadersAsync(cs, ct);
        if (headerBytes is null) return;
        var (method, target, _) = ParseRequestLine(headerBytes);

        // Connect to upstream proxy only (prevents DNS leak for destinations)
        using var up = new TcpClient();
        up.NoDelay = true;
        await up.ConnectAsync(_upHost, _upPort, ct);
        using var ups = up.GetStream();

        if (method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
        {
            // Build CONNECT request to upstream; NEVER resolve target locally
            var sb = new StringBuilder();
            sb.Append("CONNECT ").Append(target).Append(" HTTP/1.1\r\n");
            sb.Append("Host: ").Append(target).Append("\r\n");
            sb.Append("Proxy-Connection: keep-alive\r\n");
            if (_proxyAuth is not null) sb.Append(_proxyAuth);
            sb.Append("\r\n");
            var req = Encoding.ASCII.GetBytes(sb.ToString());
            await ups.WriteAsync(req, 0, req.Length, ct);
            await ups.FlushAsync(ct);

            var respHead = await ReadHeadersAsync(ups, ct) ?? Array.Empty<byte>();
            await cs.WriteAsync(respHead, 0, respHead.Length, ct);
            await cs.FlushAsync(ct);

            // Relay both directions
            await RelayDuplexAsync(cs, ups, ct);
            return;
        }
        else
        {
            // HTTP request: forward to upstream
            var hdrStr = Encoding.ASCII.GetString(headerBytes);
            if (_proxyAuth is not null &&
                hdrStr.IndexOf("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase) < 0)
            {
                int p = hdrStr.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (p > 0) hdrStr = hdrStr.Insert(p, "\r\n" + _proxyAuth.TrimEnd('\r', '\n'));
                headerBytes = Encoding.ASCII.GetBytes(hdrStr);
            }

            await ups.WriteAsync(headerBytes, 0, headerBytes.Length, ct);
            await RelayDuplexAsync(cs, ups, ct);
            return;
        }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChainedHttpProxy] Handler error: {ex.Message}");
        }
    }

    private static async Task RelayDuplexAsync(Stream a, Stream b, CancellationToken ct)
    {
        var t1 = a.CopyToAsync(b, 64 * 1024, ct);
        var t2 = b.CopyToAsync(a, 64 * 1024, ct);
        await Task.WhenAny(t1, t2);
        try { a.Dispose(); } catch { }
        try { b.Dispose(); } catch { }
    }
}
