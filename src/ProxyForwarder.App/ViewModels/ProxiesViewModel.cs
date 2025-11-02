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
    private readonly IIpWhoisClient _whois;
    private readonly INotificationService _notifications;
    private readonly SemaphoreSlim _sem = new(8); // measure up to 8 proxies in parallel
    private Timer? _pingTimer;

    [ObservableProperty] private ObservableCollection<ProxyRecord> items = new();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand PingCommand { get; }

    public ProxiesViewModel()
    {
        _repo = (IProxyRepository)App.HostInstance!.Services.GetRequiredService(typeof(IProxyRepository));
        _probe = (ILatencyProbe)App.HostInstance!.Services.GetRequiredService(typeof(ILatencyProbe));
        _whois = (IIpWhoisClient)App.HostInstance!.Services.GetRequiredService(typeof(IIpWhoisClient));
        _notifications = (INotificationService)App.HostInstance!.Services.GetRequiredService(typeof(INotificationService));
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        PingCommand = new AsyncRelayCommand(PingAllAsync);
        
        // Subscribe to proxies synced event - auto-refresh after import
        _notifications.ProxiesSynced += async (_, _) => await RefreshAsync();
        
        // Start 10-minute ping timer
        _pingTimer = new Timer(_ => _ = PingAllAsync(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var all = await _repo.GetAllAsync();
        Items = new ObservableCollection<ProxyRecord>(all);
        // Auto-populate ISP after refresh
        await PopulateIspAsync();
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
                // Measure latency
                var ms = await _probe.ProbeAsync(p.Host, p.Port, p.Username, p.Password, 8000, cts.Token);
                p.Ping = ms is null ? null : (int?)ms.Value;
                
                // Get ISP/Location from ipwho.is through proxy
                var whoisInfo = await _whois.GetIpInfoAsync(p.Host, p.Port, p.Username, p.Password, cts.Token);
                if (whoisInfo is not null)
                {
                    p.ExitIp = whoisInfo.Ip;
                    p.ISP = whoisInfo.Isp ?? whoisInfo.Organization;
                    if (!string.IsNullOrWhiteSpace(whoisInfo.Country) || !string.IsNullOrWhiteSpace(whoisInfo.City))
                    {
                        var loc = new List<string>();
                        if (!string.IsNullOrWhiteSpace(whoisInfo.City)) loc.Add(whoisInfo.City);
                        if (!string.IsNullOrWhiteSpace(whoisInfo.Country)) loc.Add(whoisInfo.Country);
                        p.Location = string.Join(" - ", loc);
                    }
                }
                
                // Force DataGrid refresh
                var idx = Items.IndexOf(p);
                if (idx >= 0)
                {
                    await App.Current!.Dispatcher.InvokeAsync(() =>
                    {
                        Items[idx] = Items[idx];
                    });
                }
            }
            catch { /* ignore per-item errors */ }
            finally { _sem.Release(); }
        });
        await Task.WhenAll(tasks);
        
        // Save all updated proxies to database
        try
        {
            await _repo.UpsertProxiesAsync(list);
        }
        catch { /* ignore save errors */ }
    }

    private async Task PopulateIspAsync()
    {
        var list = Items.ToList();
        if (list.Count == 0) return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            
            // Process proxies sequentially (not parallel) to avoid rate limiting
            foreach (var p in list)
            {
                try
                {
                    // Get ISP/Location/ExitIP from ipwho.is through proxy
                    var whoisInfo = await _whois.GetIpInfoAsync(p.Host, p.Port, p.Username, p.Password, cts.Token);
                    System.Diagnostics.Debug.WriteLine($"[PopulateIspAsync] {p.Host}:{p.Port} -> ISP={whoisInfo?.Isp}, ExitIp={whoisInfo?.Ip}");
                    if (whoisInfo is not null)
                    {
                        p.ExitIp = whoisInfo.Ip;
                        p.ISP = whoisInfo.Isp ?? whoisInfo.Organization;
                        if (!string.IsNullOrWhiteSpace(whoisInfo.Country) || !string.IsNullOrWhiteSpace(whoisInfo.City))
                        {
                            var loc = new List<string>();
                            if (!string.IsNullOrWhiteSpace(whoisInfo.City)) loc.Add(whoisInfo.City);
                            if (!string.IsNullOrWhiteSpace(whoisInfo.Country)) loc.Add(whoisInfo.Country);
                            p.Location = string.Join(" - ", loc);
                        }
                    }

                    // Force DataGrid refresh
                    var idx = Items.IndexOf(p);
                    if (idx >= 0)
                    {
                        await App.Current!.Dispatcher.InvokeAsync(() =>
                        {
                            Items[idx] = Items[idx];
                        });
                    }
                }
                catch { /* ignore per-item errors */ }
            }

            // Save all updated proxies to database
            try
            {
                await _repo.UpsertProxiesAsync(list);
            }
            catch { /* ignore save errors */ }
        }
        catch { /* ignore all errors */ }
    }
}
