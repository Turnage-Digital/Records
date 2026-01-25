using Records.Tenants.Contracts.Dtos;

namespace Records.Tenants.Contracts.Queries;

public interface ITenantQueries
{
    Task<TenantSummaryDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken cancellationToken);
}