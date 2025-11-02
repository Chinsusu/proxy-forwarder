// <copyright file="SettingsProvider.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Text.Json;
using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.Infrastructure.Services;

public sealed class SettingsProvider : ISettingsProvider
{
    private static readonly string PathFile = System.IO.Path.Combine(AppContext.BaseDirectory, "settings.json");
    public AppSettings Current { get; private set; } = new();

    public SettingsProvider()
    {
        if (File.Exists(PathFile))
        {
            try
            {
                var json = File.ReadAllText(PathFile);
                Current = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();
            }
            catch { /* keep default */ }
        }
    }

    public Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions(JsonSerializerDefaults.Web){ WriteIndented = true});
        File.WriteAllText(PathFile, json);
        return Task.CompletedTask;
    }
}
