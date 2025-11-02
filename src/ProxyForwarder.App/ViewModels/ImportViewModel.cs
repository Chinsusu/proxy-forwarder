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
    private readonly INotificationService _notifications;

    [ObservableProperty] private string token = string.Empty;
    [ObservableProperty] private string typeFilter = "proxy";
    [ObservableProperty] private string syncMessage = string.Empty;

    public IAsyncRelayCommand SyncAllCommand { get; }

    public ImportViewModel()
    {
        // Resolve via App.Host DI container
        _client = (ICloudMiniClient)App.HostInstance!.Services.GetRequiredService(typeof(ICloudMiniClient));
        _repo   = (IProxyRepository)App.HostInstance!.Services.GetRequiredService(typeof(IProxyRepository));
        _secure = (ISecureStorage)App.HostInstance!.Services.GetRequiredService(typeof(ISecureStorage));
        _notifications = (INotificationService)App.HostInstance!.Services.GetRequiredService(typeof(INotificationService));

        SyncAllCommand = new AsyncRelayCommand(SyncAllAsync);
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
            var rawsWithMetadata = await _client.GetAllProxiesWithMetadataAsync(tk, TypeFilter.Trim(), settings.Current.MaxProxiesPerSync, CancellationToken.None);
            // Clear all existing proxies before importing new ones
            await _repo.ClearAllAsync();
            
            var records = new List<ProxyRecord>();
            foreach (var (proxyStr, price, location, expirationDate, isp) in rawsWithMetadata)
            {
                if (ProxyParser.TryParse(proxyStr, out var r))
                {
                    // Classify proxy type based on price
                    r.Type = ProxyTypeClassifier.ClassifyByPrice(price);
                    r.Location = location;
                    r.ExpirationDate = expirationDate;
                    r.ISP = isp;
                    records.Add(r);
                }
            }
            await _repo.UpsertProxiesAsync(records);
            SyncMessage = $"✓ Đã sync {records.Count} proxy.";
            // Notify other ViewModels to refresh
            _notifications.NotifyProxiesSynced();
        }
        catch (HttpRequestException ex)
        {
            SyncMessage = $"✗ Sync failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            SyncMessage = $"✗ Error: {ex.Message}";
        }
    }
}
