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
            var settings = (ISettingsProvider)App.HostInstance!.Services.GetRequiredService(typeof(ISettingsProvider));
            var pageSize = 500;
            var maxCount = settings.Current.MaxProxiesPerSync;
            var taken = 0;
            var all = new List<ProxyRecord>();
            
            // Get details from /proxy endpoint to have ISP/Location/ExpireDate
            for (int page = 1; taken < maxCount; page++)
            {
                var batch = await _client.GetAccountProxiesAsync(tk, page, pageSize, CancellationToken.None);
                if (batch.Count == 0) break;
                
                foreach (var r in batch)
                {
                    all.Add(r);
                    taken++;
                    if (taken >= maxCount) break;
                }
            }
            
            // Clear all existing proxies before importing new ones
            await _repo.ClearAllAsync();
            await _repo.UpsertProxiesAsync(all);
            SyncMessage = $"✓ Đã sync {all.Count} proxy.";
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
