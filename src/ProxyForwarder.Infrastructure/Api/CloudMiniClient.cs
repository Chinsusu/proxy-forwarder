// <copyright file="CloudMiniClient.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http;
using System.Text.Json;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Infrastructure.Api;

public sealed class CloudMiniClient : ICloudMiniClient
{
    private readonly HttpClient _http;
    private readonly ISettingsProvider _settings;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CloudMiniClient(HttpClient http, ISettingsProvider settings)
    { _http = http; _settings = settings; }

    private void ApplyAuth(HttpRequestMessage req, string token)
    {
        var s = _settings.Current;
        if (string.Equals(s.AuthHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = string.IsNullOrWhiteSpace(s.AuthScheme) ? "Bearer" : s.AuthScheme;
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(scheme, token);
        }
        else
        {
            req.Headers.TryAddWithoutValidation(s.AuthHeader, 
                string.IsNullOrWhiteSpace(s.AuthScheme) ? token : $"{s.AuthScheme} {token}");
        }
    }

    private async Task<HttpResponseMessage> TryGetAsync(IEnumerable<string> paths, string token, CancellationToken ct)
    {
        foreach (var p in paths)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, p);
            ApplyAuth(req, token);
            var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.IsSuccessStatusCode) return res;
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return res;
        }
        return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = new HttpRequestMessage(HttpMethod.Get, paths.Last()) };
    }

    public async Task<IReadOnlyList<Region>> GetRegionsAsync(string token, CancellationToken ct)
    {
        var s = _settings.Current;
        var candidates = new[]
        {
            s.RegionsPath,
            "proxy/residential/regions",
            "residential/regions",
            "proxy/regions",
            "regions"
        }.Distinct().ToArray();

        using var res = await TryGetAsync(candidates, token, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = $"Regions endpoint failed (HTTP {(int)res.StatusCode}) at '{res.RequestMessage?.RequestUri}'. Hãy vào Settings để chỉnh RegionsPath.";
            throw new HttpRequestException(msg, null, res.StatusCode);
        }

        await using var sStream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(sStream, cancellationToken: ct);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return doc.RootElement.EnumerateArray()
                .Select(e => new Region { Code = e.GetProperty("code").GetString() ?? "", Name = e.GetProperty("name").GetString() ?? "" })
                .Where(r => !string.IsNullOrWhiteSpace(r.Code)).ToList();
        }
        if (doc.RootElement.TryGetProperty("regions", out var regions) || doc.RootElement.TryGetProperty("items", out regions))
        {
            return regions.EnumerateArray()
                .Select(e => new Region { Code = e.GetProperty("code").GetString() ?? "", Name = e.GetProperty("name").GetString() ?? "" })
                .Where(r => !string.IsNullOrWhiteSpace(r.Code)).ToList();
        }
        var list = new List<Region>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            list.Add(new Region{ Code = prop.Name, Name = prop.Value.GetString() ?? prop.Name });
        return list;
    }

    public async Task<IReadOnlyList<string>> GetProxiesRawAsync(string token, string regionCode, int count, CancellationToken ct)
    {
        var s = _settings.Current;
        string Format(string path) => path.Replace("{code}", Uri.EscapeDataString(regionCode)).Replace("{count}", count.ToString());

        var candidates = new[]
        {
            Format(s.ProxiesPath),
            Format("proxy/residential/proxies?region={code}&count={count}"),
            Format("residential/proxies?region={code}&count={count}"),
            Format("proxies?region={code}&count={count}")
        }.Distinct().ToArray();

        using var res = await TryGetAsync(candidates, token, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = $"Proxies endpoint failed (HTTP {(int)res.StatusCode}) at '{res.RequestMessage?.RequestUri}'. Hãy vào Settings để chỉnh ProxiesPath.";
            throw new HttpRequestException(msg, null, res.StatusCode);
        }

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
                .Where(s2 => !string.IsNullOrWhiteSpace(s2)).ToList();
        }
        if (doc.RootElement.TryGetProperty("proxies", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            return arr.EnumerateArray().Select(x => x.GetString() ?? string.Empty)
                .Where(s2 => !string.IsNullOrWhiteSpace(s2)).ToList();
        }
        return Array.Empty<string>();
    }
}
