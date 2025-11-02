// <copyright file="ForwarderService.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Forwarding;

/// <summary>
/// Forwarder service using HTTP explicit proxy chaining.
/// Prevents DNS leaks by forwarding all requests through upstream proxy (CONNECT for HTTPS, absolute-URI for HTTP).
/// No SSL MITM, no local certificate management.
/// </summary>
public sealed class ForwarderService : IForwarderService, IAsyncDisposable
{
    private readonly Dictionary<Guid, ChainedHttpProxy> _running = new();
    private readonly object _lock = new();

    public async Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_running.ContainsKey(proxy.Id)) return localPort;
        }

        try
        {
            // Parse upstream: HOST:PORT[:USER:PASS]
            var parts = proxy.Host.Split(':');
            var upHost = parts[0];
            if (!int.TryParse(parts.Length > 1 ? parts[1] : "80", out var upPort))
                upPort = 80;
            var upUser = parts.Length > 2 ? parts[2] : null;
            var upPass = parts.Length > 3 ? parts[3] : null;

            var forwarder = new ChainedHttpProxy(IPAddress.Loopback, localPort, upHost, upPort, upUser, upPass);
            forwarder.Start();

            lock (_lock)
            {
                _running[proxy.Id] = forwarder;
            }

            Debug.WriteLine($"✓ HTTP proxy chaining started: 127.0.0.1:{localPort} → {upHost}:{upPort}");
            return await Task.FromResult(localPort);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Failed to start forwarder: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync(Guid proxyId)
    {
        ChainedHttpProxy? forwarder = null;
        lock (_lock)
        {
            if (_running.Remove(proxyId, out forwarder))
            {
                Debug.WriteLine($"✓ HTTP proxy stopped for {proxyId}");
            }
        }

        if (forwarder is not null)
            await forwarder.DisposeAsync();
    }

    public bool IsRunning(Guid proxyId)
    {
        lock (_lock)
        {
            return _running.ContainsKey(proxyId);
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        List<ChainedHttpProxy> toDispose;
        lock (_lock)
        {
            toDispose = _running.Values.ToList();
            _running.Clear();
        }

        foreach (var f in toDispose)
            await f.DisposeAsync();
    }
}

public sealed class PortAllocator
{
    private readonly int _min;
    private readonly int _max;
    private int _next;

    public PortAllocator(int min = 11000, int max = 14999)
    { _min = min; _max = max; _next = min; }

    public int Next()
    {
        for (var i = 0; i < (_max - _min + 1); i++)
        {
            var p = _next++;
            if (_next > _max) _next = _min;
            if (IsFree(p)) return p;
        }
        throw new InvalidOperationException("No free ports in range");
    }

    private static bool IsFree(int port)
    {
        var ip = IPGlobalProperties.GetIPGlobalProperties();
        var tcpFree = ip.GetActiveTcpListeners().All(e => e.Port != port);
        var udpFree = ip.GetActiveUdpListeners().All(e => e.Port != port);
        return tcpFree && udpFree;
    }
}
