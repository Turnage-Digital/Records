using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;

namespace Records.Tenants.Infrastructure.Sql.Queries;

public sealed class TenantQueries(TenantsDbContext dbContext) : ITenantQueries
{
    public async Task<TenantSummaryDto?> GetByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        return await dbContext.TenantProjections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantKey)
            .Select(x => new TenantSummaryDto(
                UlidId.Parse(x.TenantId),
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
                UlidId.Parse(x.TenantId),
                x.Name,
                x.Status,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}