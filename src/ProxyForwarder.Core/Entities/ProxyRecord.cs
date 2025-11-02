using System;

namespace ProxyForwarder.Core.Entities;

public sealed class ProxyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Provider { get; set; } = "CloudMini";
    public string? Location { get; set; } // Location/Region from proxy provider
    public string? Type { get; set; } // PrivateV4, PrivateV6, Residential, ResidentialStatic, BudgetV4, etc.
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; } // When the proxy expires
    public bool Disabled { get; set; }
    
    /// <summary>Get remaining days until expiration</summary>
    public int? DaysRemaining
    {
        get
        {
            if (!ExpirationDate.HasValue) return null;
            var remaining = ExpirationDate.Value - DateTime.Now;
            return remaining.TotalSeconds < 0 ? 0 : (int)Math.Ceiling(remaining.TotalDays);
        }
    }
    
    /// <summary>Get remaining time until expiration (formatted as: 1d 2h 5')</summary>
    public string? GetRemainingTime()
    {
        if (!ExpirationDate.HasValue) return "N/A";
        
        var remaining = ExpirationDate.Value - DateTime.UtcNow;
        if (remaining.TotalSeconds < 0) return "Expired";
        
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h {remaining.Minutes}'";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}'";
        
        return $"{remaining.Minutes}'";
    }
}
