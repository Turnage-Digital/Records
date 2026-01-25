using Microsoft.EntityFrameworkCore;
using Records.Tenants.Contracts;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql.Projections;

public sealed class TenantProjectionWriter(TenantsDbContext dbContext) : ITenantProjectionWriter
{
    public async Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == model.TenantId, cancellationToken);

        if (record is null)
        {
            record = new TenantProjectionDb
            {
                TenantId = model.TenantId,
                Name = model.Name,
                Status = model.Status,
                CreatedAt = model.CreatedAt
            };
            dbContext.TenantProjections.Add(record);
        }
        else
        {
            record.Name = model.Name;
            record.Status = model.Status;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken)
    {
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNameAsync(Guid tenantId, string name, CancellationToken cancellationToken)
    {
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
