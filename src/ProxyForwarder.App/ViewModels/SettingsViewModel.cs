using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settings;

    [ObservableProperty] private int portRangeMin;
    [ObservableProperty] private int portRangeMax;
    [ObservableProperty] private bool autoStartForwarders;

    public IAsyncRelayCommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
        PortRangeMin = _settings.Current.PortRangeMin;
        PortRangeMax = _settings.Current.PortRangeMax;
        AutoStartForwarders = _settings.Current.AutoStartForwarders;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task SaveAsync()
    {
        _settings.Current.PortRangeMin = PortRangeMin;
        _settings.Current.PortRangeMax = PortRangeMax;
        _settings.Current.AutoStartForwarders = AutoStartForwarders;
        await _settings.SaveAsync();
    }
}
