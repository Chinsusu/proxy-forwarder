namespace ProxyForwarder.Core.Abstractions;

/// <summary>
/// Simple event-based notification service for ViewModels to communicate
/// </summary>
public interface INotificationService
{
    event EventHandler? ProxiesSynced;
    void NotifyProxiesSynced();
}

public sealed class NotificationService : INotificationService
{
    public event EventHandler? ProxiesSynced;

    public void NotifyProxiesSynced()
    {
        ProxiesSynced?.Invoke(this, EventArgs.Empty);
    }
}
