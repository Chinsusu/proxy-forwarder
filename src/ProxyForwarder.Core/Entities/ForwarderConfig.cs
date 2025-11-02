using System;

namespace ProxyForwarder.Core.Entities;

public sealed class ForwarderConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProxyId { get; set; }
    public int LocalPort { get; set; }
    public bool AutoStart { get; set; }
    public string Mode { get; set; } = "http";
}
