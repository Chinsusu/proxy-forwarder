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
    private readonly ILatencyProbe _probe;
    private readonly INotificationService _notifications;
    private readonly SemaphoreSlim _sem = new(8); // measure up to 8 proxies in parallel

    [ObservableProperty] private ObservableCollection<ProxyRecord> items = new();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand PingCommand { get; }

    public ProxiesViewModel()
    {
        _repo = (IProxyRepository)App.HostInstance!.Services.GetRequiredService(typeof(IProxyRepository));
        _probe = (ILatencyProbe)App.HostInstance!.Services.GetRequiredService(typeof(ILatencyProbe));
        _notifications = (INotificationService)App.HostInstance!.Services.GetRequiredService(typeof(INotificationService));
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        PingCommand = new AsyncRelayCommand(PingAllAsync);
        
        // Subscribe to proxies synced event
        _notifications.ProxiesSynced += async (_, _) => await RefreshAsync();
        
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var all = await _repo.GetAllAsync();
        Items = new ObservableCollection<ProxyRecord>(all);
    }

    private async Task PingAllAsync()
    {
        var list = Items.ToList();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var tasks = list.Select(async p =>
        {
            await _sem.WaitAsync(cts.Token);
            try
            {
                var ms = await _probe.ProbeAsync(p.Host, p.Port, p.Username, p.Password, 8000, cts.Token);
                p.Ping = ms is null ? null : (int?)ms.Value;
                // Force DataGrid refresh (replace item in collection to trigger CollectionChanged)
                var idx = Items.IndexOf(p);
                if (idx >= 0)
                {
                    await App.Current!.Dispatcher.InvokeAsync(() =>
                    {
                        Items[idx] = Items[idx]; // raises CollectionChanged
                    });
                }
            }
            catch { /* ignore per-item errors */ }
            finally { _sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }
}
