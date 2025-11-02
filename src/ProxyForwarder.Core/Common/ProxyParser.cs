using System.Globalization;
using System.Text.RegularExpressions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Common;

public static class ProxyParser
{
    private static readonly Regex Pattern = new(
        @"^(?<host>[^:\s]+):(?<port>\d{2,5})(?::(?<user>[^:\s]+):(?<pass>.+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(string raw, out ProxyRecord rec)
    {
        rec = new ProxyRecord();
        var m = Pattern.Match(raw.Trim());
        if (!m.Success) return false;
        rec.Host = m.Groups["host"].Value;
        rec.Port = int.Parse(m.Groups["port"].Value, CultureInfo.InvariantCulture);
        if (m.Groups["user"].Success) rec.Username = m.Groups["user"].Value;
        if (m.Groups["pass"].Success) rec.Password = m.Groups["pass"].Value;
        return true;
    }
}
