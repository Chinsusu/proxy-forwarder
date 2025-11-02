using System;
using System.Threading;
using System.Threading.Tasks;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Abstractions;

public interface IForwarderService
{
    Task<int> StartAsync(ProxyRecord proxy, int localPort, CancellationToken ct);
    Task StopAsync(Guid proxyId);
    bool IsRunning(Guid proxyId);
}
