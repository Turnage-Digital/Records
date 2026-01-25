using Microsoft.EntityFrameworkCore;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;

namespace Records.Tenants.Infrastructure.Sql.Queries;

public sealed class TenantQueries(TenantsDbContext dbContext) : ITenantQueries
{
    public async Task<TenantSummaryDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.TenantProjections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new TenantSummaryDto(
                x.TenantId,
                x.Name,
                x.Status,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.TenantProjections
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TenantSummaryDto(
                x.TenantId,
                x.Name,
                x.Status,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
