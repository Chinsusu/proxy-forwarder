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
    // API endpoints (configurable)
    public string RegionsPath { get; set; } = "regions";
    // Use {code} and {count} placeholders
    public string ProxiesPath { get; set; } = "proxies?region={code}&count={count}";
    // Auth
    public string AuthHeader { get; set; } = "Authorization"; // "Authorization", "X-API-Key", ...
    public string AuthScheme { get; set; } = "Bearer";        // "Bearer" or empty
}
