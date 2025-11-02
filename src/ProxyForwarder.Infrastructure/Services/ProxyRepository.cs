// <copyright file="ProxyRepository.cs" company="ProxyForwarder">
// Copyright (c) ProxyForwarder. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using ProxyForwarder.Core.Abstractions;
using ProxyForwarder.Core.Entities;
using ProxyForwarder.Infrastructure.Data;

namespace ProxyForwarder.Infrastructure.Services;

public sealed class ProxyRepository : IProxyRepository
{
    private readonly IDbContextFactory<ForwarderDbContext> _factory;
    public ProxyRepository(IDbContextFactory<ForwarderDbContext> factory) => _factory = factory;

    public async Task UpsertProxiesAsync(IReadOnlyList<ProxyRecord> items)
    {
        await using var db = await _factory.CreateDbContextAsync();
        foreach (var it in items)
        {
            var exists = await db.Proxies.FirstOrDefaultAsync(p =>
                p.Host == it.Host && p.Port == it.Port && p.Username == it.Username);
            if (exists is null)
                await db.Proxies.AddAsync(it);
        }
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ProxyRecord>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Proxies.AsNoTracking().OrderByDescending(p => p.ImportedAtUtc).ToListAsync();
    }

    public async Task ClearAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Proxies.ExecuteDeleteAsync();
    }
}
