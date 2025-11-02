using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ProxyForwarder.Infrastructure.Data;

var dbPath = Path.Combine(AppContext.BaseDirectory, "publish", "forwarder.db");
Console.WriteLine($"DB Path: {dbPath}");
Console.WriteLine($"DB Exists: {File.Exists(dbPath)}");

var options = new DbContextOptionsBuilder<ForwarderDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using (var db = new ForwarderDbContext(options))
{
    var proxies = db.Proxies.Take(3).ToList();
    Console.WriteLine($"Total proxies in DB: {db.Proxies.Count()}");
    foreach (var p in proxies)
    {
        Console.WriteLine($"Host={p.Host}, Port={p.Port}, ISP={p.ISP}, ExitIp={p.ExitIp}, Location={p.Location}");
    }
}
