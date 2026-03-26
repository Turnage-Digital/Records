using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Infrastructure.Sql.Entities;
using Records.Recordsets.Infrastructure.Sql.QueryCriteria;

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
                LastChangedAt = model.LastChangedAt
            };
            dbContext.RecordsetProjections.Add(record);
        }
        else
        {
            record.Name = model.Name;
            record.ItemCount = model.ItemCount;
            record.LastChangedAt = model.LastChangedAt;
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
        record.LastChangedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastChangedAsync(
        UlidId recordsetId,
        DateTimeOffset lastChangedAt,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var record = await GetByRecordsetIdAsync(recordsetKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.LastChangedAt = lastChangedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<RecordsetProjectionDb?> GetByRecordsetIdAsync(string recordsetId, CancellationToken cancellationToken)
    {
        return dbContext.RecordsetProjections
            .ApplyCriteria(new RecordsetProjectionByRecordsetIdCriteria(recordsetId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
