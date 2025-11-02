// <copyright file="ImportViewModel.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Common;
using ProxyForwarder.Core.Entities;
using ProxyForwarder.Infrastructure.Security;

namespace ProxyForwarder.App.ViewModels;

public partial class ImportViewModel : ObservableObject
{
    private readonly ICloudMiniClient _client;
    private readonly IProxyRepository _repo;
    private readonly ISecureStorage _secure;

    [ObservableProperty] private ObservableCollection<Region> regions = new();
    [ObservableProperty] private Region? selectedRegion;
    [ObservableProperty] private int quantity = 1;
    [ObservableProperty] private string token = string.Empty;
    [ObservableProperty] private string typeFilter = "proxy";

    public IAsyncRelayCommand LoadRegionsCommand { get; }
    public IAsyncRelayCommand ImportCommand { get; }
    public IAsyncRelayCommand SyncAllCommand { get; }

    public ImportViewModel()
    {
        // Resolve via App.Host DI container
        _client = (ICloudMiniClient)App.HostInstance!.Services.GetRequiredService(typeof(ICloudMiniClient));
        _repo   = (IProxyRepository)App.HostInstance!.Services.GetRequiredService(typeof(IProxyRepository));
        _secure = (ISecureStorage)App.HostInstance!.Services.GetRequiredService(typeof(ISecureStorage));

        LoadRegionsCommand = new AsyncRelayCommand(LoadRegionsAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync, CanImport);
        SyncAllCommand = new AsyncRelayCommand(SyncAllAsync);
    }

    private bool CanImport() => SelectedRegion is not null && Quantity > 0 && !string.IsNullOrWhiteSpace(Token);

    private async Task LoadRegionsAsync()
    {
        try
        {
            var tk = await _secure.GetTokenAsync() ?? Token;
            if (string.IsNullOrWhiteSpace(tk)) { return; }
            var list = await _client.GetRegionsAsync(tk, CancellationToken.None);
            Regions = new ObservableCollection<Region>(list);
            Token = tk;
        }
        catch (HttpRequestException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Load Regions failed");
        }
    }

    private async Task ImportAsync()
    {
        var tk = Token;
        await _secure.SaveTokenAsync(tk);
        if (SelectedRegion is null) return;
        var raws = await _client.GetProxiesRawAsync(tk, SelectedRegion.Code, Quantity, CancellationToken.None);
        var records = new List<ProxyRecord>();
        foreach (var s in raws)
        {
            if (ProxyParser.TryParse(s, out var r)) records.Add(r);
        }
        await _repo.UpsertProxiesAsync(records);
    }

    private async Task SyncAllAsync()
    {
        try
        {
            var tk = Token;
            await _secure.SaveTokenAsync(tk);
            if (string.IsNullOrWhiteSpace(TypeFilter))
            {
                System.Windows.MessageBox.Show("Nhập type filter, ví dụ: proxy");
                return;
            }
            var settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
            var raws = await _client.GetAllProxiesByTypeAsync(tk, TypeFilter.Trim(), settings.Current.MaxProxiesPerSync, CancellationToken.None);
            var records = new List<ProxyRecord>();
            foreach (var s in raws)
            {
                if (ProxyParser.TryParse(s, out var r)) records.Add(r);
            }
            await _repo.UpsertProxiesAsync(records);
            System.Windows.MessageBox.Show($"Đã sync {records.Count} proxy.");
        }
        catch (HttpRequestException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Sync ALL failed");
        }
    }
}
