using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settings;

    [ObservableProperty] private string apiBaseUrl = "";
    [ObservableProperty] private int portRangeMin;
    [ObservableProperty] private int portRangeMax;
    [ObservableProperty] private bool autoStartForwarders;
    [ObservableProperty] private string regionsPath = "";
    [ObservableProperty] private string proxiesPath = "";
    [ObservableProperty] private string authHeader = "";
    [ObservableProperty] private string authScheme = "";
    [ObservableProperty] private string allProxiesPath = "";
    [ObservableProperty] private int allProxiesPageSize;

    public IAsyncRelayCommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
        ApiBaseUrl = _settings.Current.ApiBaseUrl;
        PortRangeMin = _settings.Current.PortRangeMin;
        PortRangeMax = _settings.Current.PortRangeMax;
        AutoStartForwarders = _settings.Current.AutoStartForwarders;
        RegionsPath = _settings.Current.RegionsPath;
        ProxiesPath = _settings.Current.ProxiesPath;
        AuthHeader = _settings.Current.AuthHeader;
        AuthScheme = _settings.Current.AuthScheme;
        AllProxiesPath = _settings.Current.AllProxiesPath;
        AllProxiesPageSize = _settings.Current.AllProxiesPageSize;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task SaveAsync()
    {
        _settings.Current.ApiBaseUrl = ApiBaseUrl;
        _settings.Current.PortRangeMin = PortRangeMin;
        _settings.Current.PortRangeMax = PortRangeMax;
        _settings.Current.AutoStartForwarders = AutoStartForwarders;
        _settings.Current.RegionsPath = RegionsPath;
        _settings.Current.ProxiesPath = ProxiesPath;
        _settings.Current.AuthHeader = AuthHeader;
        _settings.Current.AuthScheme = AuthScheme;
        _settings.Current.AllProxiesPath = AllProxiesPath;
        _settings.Current.AllProxiesPageSize = AllProxiesPageSize;
        await _settings.SaveAsync();
    }
}
