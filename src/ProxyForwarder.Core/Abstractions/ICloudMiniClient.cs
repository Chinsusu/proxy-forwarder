using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Abstractions;

public interface ICloudMiniClient
{
    Task<IReadOnlyList<Region>> GetRegionsAsync(string token, CancellationToken ct);
    Task<IReadOnlyList<string>> GetProxiesRawAsync(string token, string regionCode, int count, CancellationToken ct);
    /// <summary>Sync all proxies by type (e.g. "proxy") with paging until maxCount or exhausted.</summary>
    Task<IReadOnlyList<string>> GetAllProxiesByTypeAsync(string token, string type, int maxCount, CancellationToken ct);
    /// <summary>Sync all proxies with metadata (proxy string + price + location) by type.</summary>
    Task<IReadOnlyList<(string ProxyString, int Price, string? Location)>> GetAllProxiesWithMetadataAsync(string token, string type, int maxCount, CancellationToken ct);
}
