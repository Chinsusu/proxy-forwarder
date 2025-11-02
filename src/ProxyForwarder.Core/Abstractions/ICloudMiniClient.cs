using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Abstractions;

public interface ICloudMiniClient
{
    Task<IReadOnlyList<Region>> GetRegionsAsync(string token, CancellationToken ct);
    Task<IReadOnlyList<string>> GetProxiesRawAsync(string token, string regionCode, int count, CancellationToken ct);
}
