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
    public string? RegionCode { get; set; }
    public string? Type { get; set; } // PrivateV4, PrivateV6, Residential, ResidentialStatic, BudgetV4, etc.
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Disabled { get; set; }
}
