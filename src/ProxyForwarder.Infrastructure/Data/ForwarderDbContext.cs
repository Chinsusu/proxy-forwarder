using Microsoft.EntityFrameworkCore;
using ProxyForwarder.Core.Entities;

namespace ProxyForwarder.Infrastructure.Data;

public class ForwarderDbContext : DbContext
{
    public DbSet<ProxyRecord> Proxies => Set<ProxyRecord>();
    public DbSet<ForwarderConfig> Forwarders => Set<ForwarderConfig>();
    public DbSet<Region> Regions => Set<Region>();

    public ForwarderDbContext(DbContextOptions<ForwarderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProxyRecord>().HasIndex(x => new { x.Host, x.Port, x.Username });
        b.Entity<Region>().HasKey(x => x.Code);
    }
}
