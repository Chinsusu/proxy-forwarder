using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;
using ProxyForwarder.Forwarding;

namespace ProxyForwarder.App.ViewModels;

public partial class ForwardersViewModel : ObservableObject
{
    private readonly IProxyRepository _repo;
    private readonly IForwarderService _forwarder;
    private readonly PortAllocator _ports;
    private readonly INotificationService _notifications;

    [ObservableProperty] private ObservableCollection<ForwarderRow> rows = new();

    public IAsyncRelayCommand ToggleAllCommand { get; }
    public IAsyncRelayCommand<ForwarderRow> ToggleOneCommand { get; }

    public ForwardersViewModel()
    {
        var sp = App.HostInstance!.Services;
        _repo = (IProxyRepository)sp.GetRequiredService(typeof(IProxyRepository));
        _forwarder = (IForwarderService)sp.GetRequiredService(typeof(IForwarderService));
        _ports = (PortAllocator)sp.GetRequiredService(typeof(PortAllocator));
        _notifications = (INotificationService)sp.GetRequiredService(typeof(INotificationService));

        ToggleAllCommand = new AsyncRelayCommand(ToggleAllAsync);
        ToggleOneCommand = new AsyncRelayCommand<ForwarderRow>(ToggleOneAsync);

        // Subscribe to proxies synced event to reload forwarders
        _notifications.ProxiesSynced += async (_, _) => await LoadAsync();
        
        // Load initial proxies if available
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Load proxies from in-memory storage
        var all = _notifications.GetProxies();
        // Filter out expired proxies
        var active = all.Where(p => p.ExpirationDate == null || p.ExpirationDate > DateTime.Now).ToList();
        await Task.CompletedTask; // Keep async signature
        Rows = new ObservableCollection<ForwarderRow>(active.Select(p => new ForwarderRow(p)));
    }

    private async Task ToggleAllAsync()
    {
        foreach (var r in Rows) await ToggleOneAsync(r);
    }

    private Task ToggleOneAsync(ForwarderRow? row)
    {
        if (row is null) return Task.CompletedTask;
        if (row.Status == "Running")
            return _forwarder.StopAsync(row.Proxy.Id).ContinueWith(_ => row.Status = "Stopped");
        else
        {
            if (row.LocalPort == 0) row.LocalPort = _ports.Next();
            row.Status = "Starting...";
            return Task.Run(async () =>
            {
                try
                {
                    // Wrap in try-catch to suppress any SSL/certificate popups
                    await _forwarder.StartAsync(row.Proxy, row.LocalPort, CancellationToken.None);
                    row.Status = "Running";
                }
                catch (Exception ex)
                {
                    row.Status = "Error";
                    Debug.WriteLine($"Error starting forwarder: {ex.Message}");
                }
            });
        }
    }
}

public sealed partial class ForwarderRow : ObservableObject
{
    public ProxyRecord Proxy { get; }
    public string Upstream => $"{Proxy.Host}:{Proxy.Port}";
    [ObservableProperty] private int localPort;
    [ObservableProperty] private string status = "Stopped";

    public ForwarderRow(ProxyRecord proxy) => Proxy = proxy;
}
