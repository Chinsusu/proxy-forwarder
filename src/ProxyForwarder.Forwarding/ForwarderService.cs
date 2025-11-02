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

    public async Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct)
    {
        if (_map.ContainsKey(proxy.Id)) return localPort;

        return await Task.Run(() =>
        {
            try
            {
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
                
                // Suppress certificate management completely to avoid any popups
                try
                {
                    server.CertificateManager.RemoveTrustedRootCertificate(true);
                }
                catch { /* Ignore */ }
                
                // Suppress all exceptions
                server.ExceptionFunc = ex => {
                    if (ex != null) Debug.WriteLine($"[Proxy] {ex.Message}");
                };
                
                server.Start();
                _map[proxy.Id] = (server, ep);
                return ep.Port;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ForwarderService.Start error: {ex.Message}");
                return localPort;
            }
        }, ct);
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
