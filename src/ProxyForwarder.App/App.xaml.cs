// <copyright file="App.xaml.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Forwarding;
using ProxyForwarder.Infrastructure.Api;
using ProxyForwarder.Infrastructure.Data;
using ProxyForwarder.Infrastructure.Security;
using ProxyForwarder.Infrastructure.Services;

namespace ProxyForwarder.App;

public partial class App : Application
{
    public static IHost? HostInstance { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Exception handlers to diagnose GUI issues
        this.DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.Exception.ToString(), "Unhandled UI Exception");
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.ExceptionObject?.ToString() ?? "(no details)", "Unhandled Exception");
        };

        HostInstance = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<ISettingsProvider, SettingsProvider>();

                services.AddDbContextFactory<ForwarderDbContext>(o =>
                    o.UseSqlite($"Data Source={System.IO.Path.Combine(AppContext.BaseDirectory, "forwarder.db")}"));

                services.AddHttpClient<ICloudMiniClient, CloudMiniClient>((sp, c) =>
                {
                    var settings = sp.GetRequiredService<ISettingsProvider>().Current;
                    var url = settings.ApiBaseUrl.EndsWith("/") ? settings.ApiBaseUrl : settings.ApiBaseUrl + "/";
                    c.BaseAddress = new Uri(url); // ensure trailing slash for relative paths
                    c.Timeout = TimeSpan.FromSeconds(20);
                });

                services.AddSingleton<ISecureStorage, SecureStorage>();
                services.AddSingleton<IForwarderService, ForwarderService>();
                services.AddSingleton<INotificationService, ProxyForwarder.Core.Abstractions.NotificationService>();
                services.AddSingleton<PortAllocator>(sp =>
                {
                    var s = sp.GetRequiredService<ISettingsProvider>().Current;
                    return new PortAllocator(s.PortRangeMin, s.PortRangeMax);
                });
                services.AddSingleton<IUdpBlocker, ProxyForwarder.Infrastructure.Services.UdpBlocker>();
                services.AddSingleton<ILatencyProbe, ProxyForwarder.Infrastructure.Services.LatencyProbe>();
                services.AddSingleton<IProxyRepository, ProxyRepository>();

                // Register ViewModels for dependency injection
                services.AddTransient<ProxyForwarder.App.ViewModels.ImportViewModel>();
                services.AddTransient<ProxyForwarder.App.ViewModels.ProxiesViewModel>();
                services.AddTransient<ProxyForwarder.App.ViewModels.ForwardersViewModel>();
                services.AddTransient<ProxyForwarder.App.ViewModels.LogsViewModel>();
                services.AddTransient<ProxyForwarder.App.ViewModels.SettingsViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        HostInstance.Start();

        // Initialize database
        var dbContextFactory = HostInstance.Services.GetRequiredService<IDbContextFactory<ForwarderDbContext>>();
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            dbContext.Database.EnsureCreated();
        }

        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        var window = HostInstance.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (HostInstance is not null)
        {
            try
            {
                // Dispose ForwarderService first to clean up proxy tasks
                var forwarderService = HostInstance.Services.GetService(typeof(IForwarderService)) as IAsyncDisposable;
                if (forwarderService is not null)
                {
                    await forwarderService.DisposeAsync();
                }
            }
            catch { }
            
            try
            {
                await HostInstance.StopAsync();
                HostInstance.Dispose();
            }
            catch { }
        }
        base.OnExit(e);
    }
}
