using System.Threading.Tasks;

namespace ProxyForwarder.Core.Abstractions;

public interface ISettingsProvider
{
    AppSettings Current { get; }
    Task SaveAsync();
}

public sealed class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://client.cloudmini.net/api/v2/";
    public int PortRangeMin { get; set; } = 11000;
    public int PortRangeMax { get; set; } = 14999;
    public bool AutoStartForwarders { get; set; } = false;
    // Không giải mã HTTPS mặc định (không cần cài chứng chỉ root)
    public bool InterceptHttps { get; set; } = false;
    // Harden browser: block UDP to avoid WebRTC/QUIC leaks (Chrome/Edge/Firefox)
    public bool BlockUdpForBrowsers { get; set; } = false;
    // API endpoints (configurable)
    public string RegionsPath { get; set; } = "regions";
    // Use {code} and {count} placeholders
    public string ProxiesPath { get; set; } = "proxies?region={code}&count={count}";
    // Auth (CloudMini uses "Token" scheme)
    public string AuthHeader { get; set; } = "Authorization"; // "Authorization", "X-API-Key", ...
    public string AuthScheme { get; set; } = "Token";         // "Token" for CloudMini, "Bearer" or empty for others
    // ---- Sync-all proxies ----
    // CloudMini: GET /proxy (no pagination needed, returns all)
    // placeholders: {page} {limit} {offset} (optional if server adds pagination later)
    public string AllProxiesPath { get; set; } = "proxy";
    public int AllProxiesPageSize { get; set; } = 500;
    public int MaxProxiesPerSync { get; set; } = 10000;
}

public interface IUdpBlocker
{
    Task ApplyAsync(bool enable, CancellationToken ct = default);
}
