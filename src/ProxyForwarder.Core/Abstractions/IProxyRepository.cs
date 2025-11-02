using System.Collections.Generic;
using System.Threading.Tasks;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Abstractions;

public interface IProxyRepository
{
    Task UpsertProxiesAsync(IReadOnlyList<ProxyRecord> items);
    Task<IReadOnlyList<ProxyRecord>> GetAllAsync();
}
