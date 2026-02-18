using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;
using Records.Tenants.Infrastructure.Sql.Specifications;

namespace Records.Tenants.Infrastructure.Sql;

public sealed class TenantQueries(TenantsDbContext dbContext) : ITenantQueries
{
    public async Task<TenantSummaryDto?> GetByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        var query = dbContext.TenantProjections
            .AsNoTracking()
            .ApplySpecification(new TenantProjectionByTenantIdSpec(tenantKey));

        return await query
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
