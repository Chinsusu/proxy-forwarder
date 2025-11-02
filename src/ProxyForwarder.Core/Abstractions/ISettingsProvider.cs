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
}
