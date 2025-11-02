using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProxyForwarder.Core.Entities;

public sealed class ProxyRecord : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Provider { get; set; } = "CloudMini";
    public string? Location { get; set; } // Location/Region from proxy provider
    public string? Type { get; set; } // PrivateV4, PrivateV6, Residential, ResidentialStatic, BudgetV4, etc.
    public string? ISP { get; set; } // Internet Service Provider
    
    private int? _ping; // Ping latency in milliseconds (notify so DataGrid refreshes)
    public int? Ping { get => _ping; set { _ping = value; OnPropertyChanged(); } }
    
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; } // When the proxy expires
    public bool Disabled { get; set; }
    
    /// <summary>Get remaining time until expiration (days, hours, or minutes)</summary>
    public string? DaysRemaining
    {
        get
        {
            if (!ExpirationDate.HasValue) return null;
            var remaining = ExpirationDate.Value - DateTime.Now;
            
            if (remaining.TotalSeconds < 0) return "Expired";
            if (remaining.TotalDays >= 1) return $"{(int)Math.Ceiling(remaining.TotalDays)}d";
            if (remaining.TotalHours >= 1) return $"{(int)Math.Ceiling(remaining.TotalHours)}h";
            
            return $"{(int)Math.Ceiling(remaining.TotalMinutes)}m";
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
