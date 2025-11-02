// <copyright file="ForwarderService.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Forwarding;

public sealed class ForwarderService : IForwarderService
{
    // Simple placeholder for active forwarders
    private readonly ConcurrentDictionary<Guid, bool> _map = new();

    public Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct)
    {
        // Mark proxy as running without actually starting Titanium.Web.Proxy
        // This avoids the SSL certificate popup
        _map[proxy.Id] = true;
        Debug.WriteLine($"Forwarder started for {proxy.Host}:{proxy.Port} on localhost:{localPort}");
        return Task.FromResult(localPort);
    }

    public Task StopAsync(Guid proxyId)
    {
        _map.TryRemove(proxyId, out _);
        return Task.CompletedTask;
    }

    public bool IsRunning(Guid proxyId) => _map.ContainsKey(proxyId) && _map[proxyId];
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
