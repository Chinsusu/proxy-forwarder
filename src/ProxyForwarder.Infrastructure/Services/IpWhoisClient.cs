// <copyright file="IpWhoisClient.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text.Json;

using ProxyForwarder.Core.Abstractions;

namespace ProxyForwarder.Infrastructure.Services;

/// <summary>
/// Get IP geolocation and ISP info from ipwho.is through HTTP proxy.
/// </summary>
public sealed class IpWhoisClient : IIpWhoisClient
{
    private static readonly HttpClientHandler _handler = new();

    public async Task<IpWhoisInfo?> GetIpInfoAsync(string proxyHost, int proxyPort, string? user, string? pass, CancellationToken ct = default)
    {
        using var http = new HttpClient(_handler, false);
        http.Timeout = TimeSpan.FromSeconds(8);

        try
        {
            // Setup proxy with auth
            var proxy = new System.Net.WebProxy($"http://{proxyHost}:{proxyPort}")
            {
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(user)
                    ? null
                    : new System.Net.NetworkCredential(user, pass)
            };
            _handler.Proxy = proxy;

            var res = await http.GetAsync("https://ipwho.is", HttpCompletionOption.ResponseContentRead, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
                return null;

            var ip = root.TryGetProperty("ip", out var ipEl) ? ipEl.GetString() ?? "" : "";
            var country = root.TryGetProperty("country", out var countryEl) ? countryEl.GetString() : null;
            var region = root.TryGetProperty("region", out var regionEl) ? regionEl.GetString() : null;
            var city = root.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : null;

            string? isp = null;
            string? org = null;
            if (root.TryGetProperty("connection", out var conn))
            {
                isp = conn.TryGetProperty("isp", out var ispEl) ? ispEl.GetString() : null;
                org = conn.TryGetProperty("org", out var orgEl) ? orgEl.GetString() : null;
            }

            return new IpWhoisInfo(ip, isp, org, country, region, city);
        }
        catch
        {
            return null;
        }
    }
}
