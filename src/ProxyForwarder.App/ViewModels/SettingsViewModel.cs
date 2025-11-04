using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settings;
    private readonly IUdpBlocker _udp;
    private readonly ProxyForwarder.Forwarding.PortAllocator _ports;

    [ObservableProperty] private int portRangeMin;
    [ObservableProperty] private int portRangeMax;
    [ObservableProperty] private bool autoStartForwarders;
    [ObservableProperty] private bool blockUdpForBrowsers;

    public IAsyncRelayCommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
        _udp = (IUdpBlocker)App.HostInstance!.Services.GetRequiredService(typeof(IUdpBlocker));
        _ports = (ProxyForwarder.Forwarding.PortAllocator)App.HostInstance!.Services.GetRequiredService(typeof(ProxyForwarder.Forwarding.PortAllocator));
        PortRangeMin = _settings.Current.PortRangeMin;
        PortRangeMax = _settings.Current.PortRangeMax;
        AutoStartForwarders = _settings.Current.AutoStartForwarders;
        BlockUdpForBrowsers = _settings.Current.BlockUdpForBrowsers;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task SaveAsync()
    {
        _settings.Current.PortRangeMin = PortRangeMin;
        _settings.Current.PortRangeMax = PortRangeMax;
        _settings.Current.AutoStartForwarders = AutoStartForwarders;
        _settings.Current.BlockUdpForBrowsers = BlockUdpForBrowsers;
        await _settings.SaveAsync();
        
        // Update PortAllocator with new range
        _ports.UpdateRange(PortRangeMin, PortRangeMax);
        
        // Apply or remove UDP firewall rules based on setting
        try
        {
            if (BlockUdpForBrowsers)
            {
                await _udp.ApplyAsync(true);
                System.Windows.MessageBox.Show("UDP block rules applied for Chrome/Edge/Firefox.", "Firewall");
            }
            else
            {
                await _udp.ApplyAsync(false);
                System.Windows.MessageBox.Show("UDP block rules removed.", "Firewall");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Firewall Error");
        }
    }
}
