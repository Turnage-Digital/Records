using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain.Entities;
using Records.Tenants.Domain.Interfaces;
using Records.Tenants.Infrastructure.Sql.Entities;
using Records.Tenants.Infrastructure.Sql.Mappers;

namespace Records.Tenants.Infrastructure.Sql;

public sealed class TenantsUnitOfWork(TenantsDbContext dbContext) : ITenantsUnitOfWork
{
    private readonly Dictionary<string, (Tenant Domain, TenantDb Entity)> trackedTenants = new(StringComparer.Ordinal);

    public void AddTenant(Tenant tenant)
    {
        var entity = TenantMapper.ToDb(tenant);
        dbContext.Tenants.Add(entity);
        trackedTenants[entity.Id] = (tenant, entity);
    }

    public async Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        var key = tenantId.ToString();

        if (trackedTenants.TryGetValue(key, out var tracked))
        {
            return tracked.Domain;
        }

        var entity = await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == key, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var domain = TenantMapper.ToDomain(entity);
        trackedTenants[key] = (domain, entity);
        return domain;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var tracked in trackedTenants.Values)
        {
            TenantMapper.UpdateDb(tracked.Domain, tracked.Entity);
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
