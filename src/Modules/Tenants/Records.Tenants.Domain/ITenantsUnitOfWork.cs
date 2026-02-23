using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Domain;

public interface ITenantsUnitOfWork
{
    void AddTenant(Tenant tenant);
    Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}