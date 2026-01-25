using Records.Tenants.Domain.Entities;

namespace Records.Tenants.Domain.Interfaces;

public interface ITenantsUnitOfWork
{
    void AddTenant(Tenant tenant);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
