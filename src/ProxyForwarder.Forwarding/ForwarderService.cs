// <copyright file="ForwarderService.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace ProxyForwarder.Forwarding;

public sealed class ForwarderService : IForwarderService
{
    private readonly ConcurrentDictionary<Guid, (ProxyServer server, ExplicitProxyEndPoint ep)> _map = new();

    public Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct)
    {
        if (_map.ContainsKey(proxy.Id)) return Task.FromResult(localPort);

        var server = new ProxyServer();
        var upstream = new ExternalProxy
        {
            HostName = proxy.Host,
            Port = proxy.Port,
            UserName = proxy.Username,
            Password = proxy.Password
        };
        server.UpStreamHttpProxy = upstream;
        server.UpStreamHttpsProxy = upstream;

        // Tạo endpoint cục bộ KHÔNG giải mã SSL (chỉ tunnel CONNECT)
        var ep = new ExplicitProxyEndPoint(IPAddress.Loopback, localPort, decryptSsl: false);
        server.AddEndPoint(ep);
        // Suppress all exceptions including certificate-related ones
        server.ExceptionFunc = ex => {
            // Silent fail - don't show popups or write to console
            if (ex != null) Debug.WriteLine($"[Proxy] {ex.Message}");
        };
        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            // Catch any SSL/certificate exceptions and suppress them
            Debug.WriteLine($"ForwarderService.Start error: {ex.Message}");
        }
        _map[proxy.Id] = (server, ep);
        return Task.FromResult(ep.Port);
    }

    public Task StopAsync(Guid proxyId)
    {
        if (_map.TryRemove(proxyId, out var x))
        {
            x.server.Stop();
            x.server.Dispose();
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
