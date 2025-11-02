using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.App.ViewModels;

public partial class ProxiesViewModel : ObservableObject
{
    private readonly IProxyRepository _repo;
    private readonly INotificationService _notifications;

    [ObservableProperty] private ObservableCollection<ProxyRecord> items = new();

    public IAsyncRelayCommand RefreshCommand { get; }

    public ProxiesViewModel()
    {
        _repo = (IProxyRepository)App.HostInstance!.Services.GetRequiredService(typeof(IProxyRepository));
        _notifications = (INotificationService)App.HostInstance!.Services.GetRequiredService(typeof(INotificationService));
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        
        // Subscribe to proxies synced event
        _notifications.ProxiesSynced += async (_, _) => await RefreshAsync();
        
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var all = await _repo.GetAllAsync();
        Items = new ObservableCollection<ProxyRecord>(all);
    }
}
