using System.Collections.ObjectModel;
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

    [ObservableProperty] private ObservableCollection<ForwarderRow> rows = new();

    public IAsyncRelayCommand StartAllCommand { get; }
    public IAsyncRelayCommand StopAllCommand { get; }
    public IAsyncRelayCommand<ForwarderRow> StartOneCommand { get; }
    public IAsyncRelayCommand<ForwarderRow> StopOneCommand { get; }

    public ForwardersViewModel()
    {
        var sp = App.HostInstance!.Services;
        _repo = (IProxyRepository)sp.GetRequiredService(typeof(IProxyRepository));
        _forwarder = (IForwarderService)sp.GetRequiredService(typeof(IForwarderService));
        _ports = (PortAllocator)sp.GetRequiredService(typeof(PortAllocator));

        StartAllCommand = new AsyncRelayCommand(StartAllAsync);
        StopAllCommand = new AsyncRelayCommand(StopAllAsync);
        StartOneCommand = new AsyncRelayCommand<ForwarderRow>(StartOneAsync);
        StopOneCommand = new AsyncRelayCommand<ForwarderRow>(StopOneAsync);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var all = await _repo.GetAllAsync();
        Rows = new ObservableCollection<ForwarderRow>(all.Select(p => new ForwarderRow(p)));
    }

    private async Task StartAllAsync()
    {
        foreach (var r in Rows) await StartOneAsync(r);
    }

    private async Task StopAllAsync()
    {
        foreach (var r in Rows) await StopOneAsync(r);
    }

    private Task StartOneAsync(ForwarderRow? row)
    {
        if (row is null) return Task.CompletedTask;
        if (row.LocalPort == 0) row.LocalPort = _ports.Next();
        return _forwarder.StartAsync(row.Proxy, row.LocalPort, CancellationToken.None)
            .ContinueWith(_ => row.Status = "Running");
    }

    private Task StopOneAsync(ForwarderRow? row)
    {
        if (row is null) return Task.CompletedTask;
        return _forwarder.StopAsync(row.Proxy.Id).ContinueWith(_ => row.Status = "Stopped");
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
