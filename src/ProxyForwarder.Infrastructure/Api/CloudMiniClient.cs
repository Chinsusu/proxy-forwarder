// <copyright file="CloudMiniClient.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text.Json;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Infrastructure.Api;

public sealed class CloudMiniClient : ICloudMiniClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CloudMiniClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Region>> GetRegionsAsync(string token, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "regions");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        await using var s = await res.Content.ReadAsStreamAsync(ct);
        var regions = await JsonSerializer.DeserializeAsync<List<Region>>(s, _json, ct) ?? new();
        return regions;
    }

    public async Task<IReadOnlyList<string>> GetProxiesRawAsync(string token, string regionCode, int count, CancellationToken ct)
    {
        var url = $"proxies?region={Uri.EscapeDataString(regionCode)}&count={count}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, _json);
            if (list is not null) return list;
        }
        catch { }
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            return items.EnumerateArray().Select(x => x.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
        return Array.Empty<string>();
    }
}
