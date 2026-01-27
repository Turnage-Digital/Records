using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain.Entities;
using Records.Tenants.Domain.Interfaces;

namespace Records.Tenants.Infrastructure.Sql;

public sealed class TenantsUnitOfWork(TenantsDbContext dbContext) : ITenantsUnitOfWork
{
    public void AddTenant(Tenant tenant)
    {
        dbContext.Tenants.Add(tenant);
    }

    public Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}