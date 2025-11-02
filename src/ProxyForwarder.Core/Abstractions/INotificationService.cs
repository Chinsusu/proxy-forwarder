using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Abstractions;

/// <summary>
/// Simple event-based notification service for ViewModels to communicate
/// </summary>
public interface INotificationService
{
    event EventHandler? ProxiesSynced;
    event EventHandler<string>? MessageChanged;
    void NotifyProxiesSynced();
    void ShowMessage(string message);
    void SetProxies(IReadOnlyList<ProxyRecord> proxies);
    IReadOnlyList<ProxyRecord> GetProxies();
}

public sealed class NotificationService : INotificationService
{
    private IReadOnlyList<ProxyRecord> _proxies = new List<ProxyRecord>();
    
    public event EventHandler? ProxiesSynced;
    public event EventHandler<string>? MessageChanged;

    public void NotifyProxiesSynced()
    {
        ProxiesSynced?.Invoke(this, EventArgs.Empty);
    }

    public void ShowMessage(string message)
    {
        MessageChanged?.Invoke(this, message);
    }
    
    public void SetProxies(IReadOnlyList<ProxyRecord> proxies)
    {
        _proxies = proxies ?? new List<ProxyRecord>();
    }
    
    public IReadOnlyList<ProxyRecord> GetProxies() => _proxies;
}
