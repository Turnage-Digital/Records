using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Infrastructure.Sql.Entities;
using Records.Recordsets.Infrastructure.Sql.Mappers;

namespace Records.Recordsets.Infrastructure.Sql.Repositories;

public sealed class RecordsetRepository(RecordsetsDbContext dbContext) : IRecordsetsUnitOfWork
{
    public async Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await dbContext.Recordsets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == recordsetKey, cancellationToken);

        if (recordset is null)
        {
            return null;
        }

        var columns = await dbContext.RecordsetColumns
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        var statuses = await dbContext.RecordsetStatuses
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        var transitions = await dbContext.RecordsetStatusTransitions
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);

        return RecordsetMapper.ToDomain(recordset, columns, statuses, transitions);
    }

    public async Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var recordset = await dbContext.Recordsets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (recordset is null)
        {
            return null;
        }

        return await GetRecordsetByIdAsync(UlidId.Parse(recordset.Id), cancellationToken);
    }

    public async Task AddRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        var recordsetDb = RecordsetMapper.ToDb(recordset);
        await dbContext.Recordsets.AddAsync(recordsetDb, cancellationToken);

        await ReplaceSchemaAsync(recordset, cancellationToken);
    }

    public Task UpdateRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        dbContext.Recordsets.Update(RecordsetMapper.ToDb(recordset));
        return ReplaceSchemaAsync(recordset, cancellationToken);
    }

    public async Task DeleteRecordsetAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await dbContext.Recordsets
            .FirstOrDefaultAsync(x => x.Id == recordsetKey, cancellationToken);
        if (recordset is null)
        {
            return;
        }

        var items = await dbContext.RecordsetItems
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetItems.RemoveRange(items);

        var columns = await dbContext.RecordsetColumns
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetColumns.RemoveRange(columns);

        var statuses = await dbContext.RecordsetStatuses
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatuses.RemoveRange(statuses);

        var transitions = await dbContext.RecordsetStatusTransitions
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatusTransitions.RemoveRange(transitions);

        var projections = await dbContext.RecordsetProjections
            .Where(x => x.RecordsetId == recordsetKey)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetProjections.RemoveRange(projections);

        dbContext.Recordsets.Remove(recordset);
    }

    public async Task AddRecordAsync(Record record, CancellationToken cancellationToken)
    {
        var entity = new RecordDb
        {
            RecordsetId = record.RecordsetId.ToString(),
            BagJson = JsonSerializer.Serialize(record.Bag),
            CreatedBy = record.CreatedBy.ToString(),
            CreatedAt = record.CreatedAt
        };
        await dbContext.RecordsetItems.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateRecordAsync(Record record, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RecordsetItems
            .FirstOrDefaultAsync(x => x.RecordsetId == record.RecordsetId.ToString() && x.Id == record.Id,
                cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.BagJson = JsonSerializer.Serialize(record.Bag);
        existing.UpdatedBy = record.UpdatedBy?.ToString();
        existing.UpdatedAt = record.UpdatedAt;
    }

    public async Task<Record?> GetRecordByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
    {
        var item = await dbContext.RecordsetItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RecordsetId == recordsetId.ToString() && x.Id == recordId,
                cancellationToken);

        if (item is null)
        {
            return null;
        }

        var bag = JsonSerializer.Deserialize<object>(item.BagJson) ?? new object();
        var domainItem = new Record(
            (int)item.Id,
            UlidId.Parse(item.RecordsetId),
            bag,
            UlidId.Parse(item.CreatedBy),
            item.CreatedAt);
        domainItem.LoadUpdated(
            item.UpdatedBy == null ? null : UlidId.Parse(item.UpdatedBy),
            item.UpdatedAt);
        return domainItem;
    }

    public Task<int> GetRecordCountAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return dbContext.RecordsetItems.CountAsync(x => x.RecordsetId == recordsetKey, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceSchemaAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        var recordsetId = recordset.Id.ToString();

        var existingColumns = await dbContext.RecordsetColumns
            .Where(x => x.RecordsetId == recordsetId)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetColumns.RemoveRange(existingColumns);

        var existingStatuses = await dbContext.RecordsetStatuses
            .Where(x => x.RecordsetId == recordsetId)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatuses.RemoveRange(existingStatuses);

        var existingTransitions = await dbContext.RecordsetStatusTransitions
            .Where(x => x.RecordsetId == recordsetId)
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatusTransitions.RemoveRange(existingTransitions);

        foreach (var column in recordset.Columns)
        {
            dbContext.RecordsetColumns.Add(RecordsetMapper.ToDb(recordsetId, column));
        }

        foreach (var status in recordset.Statuses)
        {
            dbContext.RecordsetStatuses.Add(RecordsetMapper.ToDb(recordsetId, status));
        }

        foreach (var transition in recordset.StatusTransitions)
        {
            dbContext.RecordsetStatusTransitions.Add(RecordsetMapper.ToDb(recordsetId, transition));
        }
    }
}