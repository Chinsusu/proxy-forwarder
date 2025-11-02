using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
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
        HostInstance = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContextFactory<ForwarderDbContext>(o =>
                    o.UseSqlite($"Data Source={System.IO.Path.Combine(AppContext.BaseDirectory, "forwarder.db")}"));
                services.AddHttpClient<ICloudMiniClient, CloudMiniClient>(c =>
                {
                    c.BaseAddress = new Uri("https://client.cloudmini.net/api/v2/");
                    c.Timeout = TimeSpan.FromSeconds(20);
                });
                services.AddSingleton<ISecureStorage, SecureStorage>();
                services.AddSingleton<IForwarderService, ForwarderService>();
                services.AddSingleton<PortAllocator>();
                services.AddSingleton<IProxyRepository, ProxyRepository>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        HostInstance.Start();
        var window = HostInstance.Services.GetRequiredService<MainWindow>();
        window.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (HostInstance is not null)
        {
            await HostInstance.StopAsync();
            HostInstance.Dispose();
        }
        base.OnExit(e);
    }
}
