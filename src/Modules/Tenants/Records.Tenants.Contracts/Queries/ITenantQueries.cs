using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts.Dtos;

namespace Records.Tenants.Contracts.Queries;

public interface ITenantQueries
{
    Task<TenantSummaryDto?> GetByIdAsync(UlidId tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken cancellationToken);
}