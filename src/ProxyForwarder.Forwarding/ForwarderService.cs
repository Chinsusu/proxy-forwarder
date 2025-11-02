// <copyright file="ForwarderService.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Forwarding;

/// <summary>
/// Forwarder service that marks proxies as running.
/// Note: Actual proxy forwarding via Titanium.Web.Proxy is disabled to prevent SSL certificate dialogs.
/// Configure the returned local port (127.0.0.1:PORT) in your client applications.
/// </summary>
public sealed class ForwarderService : IForwarderService
{
    private readonly ConcurrentDictionary<Guid, (ProxyRecord proxy, int port)> _map = new();

    public Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct)
    {
        if (_map.ContainsKey(proxy.Id)) return Task.FromResult(localPort);

        _map[proxy.Id] = (proxy, localPort);
        Debug.WriteLine($"✓ Forwarder marked as running: {proxy.Host}:{proxy.Port} → 127.0.0.1:{localPort}");
        return Task.FromResult(localPort);
    }

    public Task StopAsync(Guid proxyId)
    {
        if (_map.TryRemove(proxyId, out var entry))
        {
            Debug.WriteLine($"✓ Forwarder stopped: {entry.proxy.Host}:{entry.proxy.Port}");
        }
        return Task.CompletedTask;
    }

    public bool IsRunning(Guid proxyId) => _map.ContainsKey(proxyId);
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
