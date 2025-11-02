using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settings;
    private readonly IUdpBlocker _udp;

    [ObservableProperty] private int portRangeMin;
    [ObservableProperty] private int portRangeMax;
    [ObservableProperty] private bool autoStartForwarders;
    [ObservableProperty] private bool blockUdpForBrowsers;

    public IAsyncRelayCommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
        _udp = (IUdpBlocker)App.HostInstance!.Services.GetRequiredService(typeof(IUdpBlocker));
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
        
        try
        {
            await _udp.ApplyAsync(BlockUdpForBrowsers);
            if (BlockUdpForBrowsers)
                System.Windows.MessageBox.Show("UDP block rules applied for Chrome/Edge/Firefox.", "Firewall");
            else
                System.Windows.MessageBox.Show("UDP block rules removed.", "Firewall");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Firewall Error");
        }
    }
}
