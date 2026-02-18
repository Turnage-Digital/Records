using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Infrastructure.Sql.Entities;
using Records.Recordsets.Infrastructure.Sql.Specifications;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetProjectionWriter(RecordsetsDbContext dbContext) : IRecordsetProjectionWriter
{
    public async Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken)
    {
        var recordsetKey = model.RecordsetId.ToString();
        var record = await GetByRecordsetIdAsync(recordsetKey, cancellationToken);

        if (record is null)
        {
            record = new RecordsetProjectionDb
            {
                RecordsetId = recordsetKey,
                Name = model.Name,
                ItemCount = model.ItemCount,
                UpdatedAt = model.UpdatedAt
            };
            dbContext.RecordsetProjections.Add(record);
        }
        else
        {
            record.Name = model.Name;
            record.ItemCount = model.ItemCount;
            record.UpdatedAt = model.UpdatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var record = await GetByRecordsetIdAsync(recordsetKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.ItemCount = itemCount;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastUpdatedAsync(
        UlidId recordsetId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var record = await GetByRecordsetIdAsync(recordsetKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.UpdatedAt = updatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<RecordsetProjectionDb?> GetByRecordsetIdAsync(string recordsetId, CancellationToken cancellationToken)
    {
        return dbContext.RecordsetProjections
            .ApplySpecification(new RecordsetProjectionByRecordsetIdSpec(recordsetId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
