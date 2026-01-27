using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql.Projections;

public sealed class TenantProjectionWriter(TenantsDbContext dbContext) : ITenantProjectionWriter
{
    public async Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken)
    {
        var tenantKey = model.TenantId.ToString();
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == tenantKey, cancellationToken);

        if (record is null)
        {
            record = new TenantProjectionDb
            {
                TenantId = tenantKey,
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

    public async Task UpdateStatusAsync(UlidId tenantId, TenantStatus status, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == tenantKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNameAsync(UlidId tenantId, string name, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        var record = await dbContext.TenantProjections
            .FirstOrDefaultAsync(x => x.TenantId == tenantKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}