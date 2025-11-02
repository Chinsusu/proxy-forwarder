// <copyright file="LatencyProbe.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.Infrastructure.Services;

/// <summary>
/// Measures latency through upstream HTTP proxy (no MITM, no local DNS resolution).
/// </summary>
public sealed class LatencyProbe : ILatencyProbe
{
    private static async Task<byte[]?> ReadHeadersAsync(NetworkStream s, CancellationToken ct, int max = 64 * 1024)
    {
        var buf = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var ms = new MemoryStream();
            int match = 0;
            while (true)
            {
                int n = await s.ReadAsync(buf.AsMemory(0, buf.Length), ct);
                if (n == 0) return null;
                ms.Write(buf, 0, n);
                
                // Find \r\n\r\n
                var data = ms.GetBuffer();
                int start = (int)Math.Max(0, ms.Length - n - 4);
                for (int i = start; i < ms.Length; i++)
                {
                    byte b = data[i];
                    match = match switch
                    {
                        0 => b == (byte)'\r' ? 1 : 0,
                        1 => b == (byte)'\n' ? 2 : 0,
                        2 => b == (byte)'\r' ? 3 : 0,
                        3 => b == (byte)'\n' ? 4 : 0,
                        _ => 0
                    };
                    if (match == 4) return ms.ToArray();
                }
                if (ms.Length > max) return ms.ToArray();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private static string? BuildProxyAuth(string? user, string? pass)
    {
        if (string.IsNullOrWhiteSpace(user)) return null;
        var b64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass ?? ""}"));
        return $"Proxy-Authorization: Basic {b64}\r\n";
    }

    public async Task<long?> ProbeAsync(string proxyHost, int proxyPort, string? user, string? pass, int timeoutMs = 8000, CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linkedCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
        ct = linkedCts.Token;

        try
        {
            var auth = BuildProxyAuth(user, pass);
            using var tcp = new TcpClient();
            tcp.NoDelay = true;
            var sw = Stopwatch.StartNew();
            await tcp.ConnectAsync(proxyHost, proxyPort, ct);
            using var ns = tcp.GetStream();

            // Prefer CONNECT so upstream resolves (no DNS leak)
            var sb = new StringBuilder();
            sb.Append("CONNECT www.google.com:443 HTTP/1.1\r\n");
            sb.Append("Host: www.google.com:443\r\n");
            sb.Append("Proxy-Connection: keep-alive\r\n");
            if (auth is not null) sb.Append(auth);
            sb.Append("\r\n");
            var req = Encoding.ASCII.GetBytes(sb.ToString());
            await ns.WriteAsync(req, 0, req.Length, ct);
            await ns.FlushAsync(ct);
            var head = await ReadHeadersAsync(ns, ct);
            if (head is not null)
            {
                var line = Encoding.ASCII.GetString(head, 0, Math.Min(head.Length, 64));
                if (line.StartsWith("HTTP/1.1 200") || line.StartsWith("HTTP/1.0 200"))
                {
                    sw.Stop();
                    return sw.ElapsedMilliseconds;
                }
            }

            // Fallback: HTTP GET with absolute URI (upstream still resolves)
            sw.Restart();
            var get = new StringBuilder();
            get.Append("GET http://www.google.com/generate_204 HTTP/1.1\r\n");
            get.Append("Host: www.google.com\r\n");
            get.Append("Connection: close\r\n");
            if (auth is not null) get.Append(auth);
            get.Append("\r\n");
            var getBytes = Encoding.ASCII.GetBytes(get.ToString());
            await ns.WriteAsync(getBytes, 0, getBytes.Length, ct);
            var head2 = await ReadHeadersAsync(ns, ct);
            sw.Stop();
            return head2 is null ? null : sw.ElapsedMilliseconds;
        }
        catch
        {
            return null;
        }
    }
}
