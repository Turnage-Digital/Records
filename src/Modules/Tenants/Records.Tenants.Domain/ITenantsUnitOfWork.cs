using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Domain;

public interface ITenantsUnitOfWork : IUnitOfWork
{
    void AddTenant(Tenant tenant);
    Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken);
}