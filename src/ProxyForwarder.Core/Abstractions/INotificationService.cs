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
}

public sealed class NotificationService : INotificationService
{
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
}
