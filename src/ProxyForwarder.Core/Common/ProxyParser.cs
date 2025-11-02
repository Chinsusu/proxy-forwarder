using System.Globalization;
using System.Text.RegularExpressions;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Core.Common;

public static class ProxyParser
{
    // Matches either:
    // 1. HOSTNAME:PORT:USER:PASS (standard format)
    // 2. HOSTNAME:ID:PORT:USER:PASS (format with ID to be stripped)
    private static readonly Regex PatternStandard = new(
        @"^(?<host>[^:\s]+):(?<port>\d{2,5})(?::(?<user>[^:\s]+):(?<pass>.+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
    private static readonly Regex PatternWithId = new(
        @"^(?<host>[^:\s]+):\d+:(?<port>\d{2,5})(?::(?<user>[^:\s]+):(?<pass>.+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(string raw, out ProxyRecord rec)
    {
        rec = new ProxyRecord();
        var trimmed = raw.Trim();
        
        // Try with ID format first (HOSTNAME:ID:PORT:USER:PASS)
        var m = PatternWithId.Match(trimmed);
        if (m.Success)
        {
            rec.Host = m.Groups["host"].Value;
            rec.Port = int.Parse(m.Groups["port"].Value, CultureInfo.InvariantCulture);
            if (m.Groups["user"].Success) rec.Username = m.Groups["user"].Value;
            if (m.Groups["pass"].Success) rec.Password = m.Groups["pass"].Value;
            return true;
        }
        
        // Fall back to standard format (HOSTNAME:PORT:USER:PASS)
        m = PatternStandard.Match(trimmed);
        if (m.Success)
        {
            rec.Host = m.Groups["host"].Value;
            rec.Port = int.Parse(m.Groups["port"].Value, CultureInfo.InvariantCulture);
            if (m.Groups["user"].Success) rec.Username = m.Groups["user"].Value;
            if (m.Groups["pass"].Success) rec.Password = m.Groups["pass"].Value;
            return true;
        }
        
        return false;
    }
}
