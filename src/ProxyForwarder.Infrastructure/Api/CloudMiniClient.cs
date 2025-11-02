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
        HttpResponseMessage? last = null;
        foreach (var relative in paths)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, relative);
            ApplyAuth(req, token);
            var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (res.IsSuccessStatusCode) return res;
            last = res;
            if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                break;
        }
        return last ?? new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static IEnumerable<string> Candidates(string main, params string[] fallbacks)
        => new[] { main }.Concat(fallbacks).Where(s => !string.IsNullOrWhiteSpace(s));

    public async Task<IReadOnlyList<Region>> GetRegionsAsync(string token, CancellationToken ct)
    {
        var s = _settings.Current;
        var fallbacks = new[]
        {
            "proxy/residential/regions",
            "residential/regions",
            "proxy/regions",
            "regions"
        };
        using var res = await TryGetAsync(Candidates(s.RegionsPath, fallbacks), token, ct);
        if (!res.IsSuccessStatusCode)
        {
            var tried = string.Join(", ", Candidates(s.RegionsPath, fallbacks).Select(p => new Uri(_http.BaseAddress!, p)));
            var msg = $"Regions endpoint failed (HTTP {(int)res.StatusCode}). Tried: {tried}";
            throw new HttpRequestException(msg, null, res.StatusCode);
        }
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return doc.RootElement.EnumerateArray()
                .Select(e => new Region {
                    Code = e.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "",
                    Name = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "" })
                .Where(r => !string.IsNullOrWhiteSpace(r.Code)).ToList();
        }
        if (doc.RootElement.TryGetProperty("regions", out var arr) ||
            doc.RootElement.TryGetProperty("items", out arr))
        {
            return arr.EnumerateArray()
                .Select(e => new Region {
                    Code = e.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "",
                    Name = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "" })
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
        string F(string p) => p.Replace("{code}", Uri.EscapeDataString(regionCode)).Replace("{count}", count.ToString());
        var fallbacks = new[]
        {
            F("proxy/residential/proxies?region={code}&count={count}"),
            F("residential/proxies?region={code}&count={count}"),
            F("proxies?region={code}&count={count}")
        };
        using var res = await TryGetAsync(Candidates(F(s.ProxiesPath), fallbacks), token, ct);
        if (!res.IsSuccessStatusCode)
        {
            var tried = string.Join(", ", Candidates(F(s.ProxiesPath), fallbacks).Select(p => new Uri(_http.BaseAddress!, p)));
            var msg = $"Proxies endpoint failed (HTTP {(int)res.StatusCode}). Tried: {tried}";
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
            return items.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s2 => !string.IsNullOrWhiteSpace(s2)).ToList();
        if (doc.RootElement.TryGetProperty("proxies", out var arr) && arr.ValueKind == JsonValueKind.Array)
            return arr.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s2 => !string.IsNullOrWhiteSpace(s2)).ToList();
        return Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetAllProxiesByTypeAsync(string token, string type, int maxCount, CancellationToken ct)
    {
        var s = _settings.Current;
        int page = 1, imported = 0;
        int limit = Math.Max(1, s.AllProxiesPageSize);
        var result = new List<string>(capacity: Math.Max(limit, maxCount));

        string F(int p, int off) =>
            s.AllProxiesPath.Replace("{type}", Uri.EscapeDataString(type))
                            .Replace("{page}", p.ToString())
                            .Replace("{limit}", limit.ToString())
                            .Replace("{offset}", off.ToString());
        string[] candidates(int p) => new[]
        {
            F(p, (p-1)*limit),
            $"proxies?type={Uri.EscapeDataString(type)}&page={p}&limit={limit}",
            $"proxies?type={Uri.EscapeDataString(type)}&limit={limit}&offset={(p-1)*limit}",
            $"proxy/list?type={Uri.EscapeDataString(type)}&page={p}&size={limit}",
            $"proxy?type={Uri.EscapeDataString(type)}&page={p}&limit={limit}",
            $"proxies?page={p}&limit={limit}"
        };

        while (imported < maxCount)
        {
            using var res = await TryGetAsync(candidates(page), token, ct);
            if (!res.IsSuccessStatusCode)
            {
                if (page == 1)
                {
                    var tried = string.Join(", ", candidates(page).Select(pth => new Uri(_http.BaseAddress!, pth)));
                    throw new HttpRequestException($"All-proxies endpoint failed (HTTP {(int)res.StatusCode}). Tried: {tried}", null, res.StatusCode);
                }
                break;
            }
            var payload = await res.Content.ReadAsStringAsync(ct);
            var pageItems = ParseProxiesFlexible(payload);
            if (pageItems.Count == 0) break;
            foreach (var sItem in pageItems)
            {
                result.Add(sItem);
                imported++;
                if (imported >= maxCount) break;
            }
            if (pageItems.Count < limit) break;
            page++;
        }
        return result;
    }

    private static List<string> ParseProxiesFlexible(string payload)
    {
        if (!payload.TrimStart().StartsWith("[") && !payload.TrimStart().StartsWith("{"))
        {
            return payload.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
        var list = new List<string>();
        using var doc = JsonDocument.Parse(payload);
        void PushIf(string? s) { if (!string.IsNullOrWhiteSpace(s)) list.Add(s!); }
        string JoinObj(JsonElement o)
        {
            string host = o.TryGetProperty("host", out var h) ? h.GetString() ?? "" :
                          o.TryGetProperty("ip", out h) ? h.GetString() ?? "" :
                          o.TryGetProperty("address", out h) ? h.GetString() ?? "" : "";
            int port = o.TryGetProperty("port", out var p) && p.TryGetInt32(out var pi) ? pi : 0;
            string? user = o.TryGetProperty("user", out var u) ? u.GetString() :
                           o.TryGetProperty("username", out u) ? u.GetString() :
                           o.TryGetProperty("login", out u) ? u.GetString() : null;
            string? pass = o.TryGetProperty("pass", out var pw) ? pw.GetString() :
                           o.TryGetProperty("password", out pw) ? pw.GetString() :
                           o.TryGetProperty("pwd", out pw) ? pw.GetString() : null;
            if (string.IsNullOrWhiteSpace(host) || port == 0) return "";
            return (user is null || pass is null) ? $"{host}:{port}" : $"{host}:{port}:{user}:{pass}";
        }
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
                PushIf(el.ValueKind == JsonValueKind.String ? el.GetString() : JoinObj(el));
            return list;
        }
        static bool TryGetArray(JsonElement root, out JsonElement arr)
        {
            string[] keys = ["items","proxies","result","records","data","list"];
            foreach (var k in keys)
            {
                if (root.TryGetProperty(k, out arr))
                {
                    if (arr.ValueKind == JsonValueKind.Array) return true;
                    if (arr.ValueKind == JsonValueKind.Object && arr.TryGetProperty("items", out var arr2) && arr2.ValueKind == JsonValueKind.Array)
                    { arr = arr2; return true; }
                }
            }
            arr = default; return false;
        }
        if (TryGetArray(doc.RootElement, out var a))
        {
            foreach (var el in a.EnumerateArray())
                PushIf(el.ValueKind == JsonValueKind.String ? el.GetString() : JoinObj(el));
        }
        return list;
    }
}
