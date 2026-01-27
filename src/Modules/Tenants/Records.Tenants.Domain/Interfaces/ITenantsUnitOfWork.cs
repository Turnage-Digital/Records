using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain.Entities;

namespace Records.Tenants.Domain.Interfaces;

public interface ITenantsUnitOfWork
{
    void AddTenant(Tenant tenant);
    Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}