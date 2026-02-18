using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Records.Core.Contracts;
using Records.Core.Contracts.Events;
using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Infrastructure.Sql.Entities;
using Records.Recordsets.Infrastructure.Sql.Mappers;
using Records.Recordsets.Infrastructure.Sql.Specifications;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetsUnitOfWork : UnitOfWork<RecordsetsDbContext>, IRecordsetsUnitOfWork
{
    private readonly RecordsetsDbContext dbContext;
    private readonly List<(Record Record, RecordDb Entity)> pendingRecords = [];

    public RecordsetsUnitOfWork(
        RecordsetsDbContext dbContext,
        IMediator mediator,
        IEventStore? eventStore = null,
        IDomainEventSerializer? serializer = null,
        ITenantContext? tenantContext = null
    )
        : base(dbContext, mediator, eventStore, serializer, tenantContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await dbContext.Recordsets
            .AsNoTracking()
            .ApplySpecification(new RecordsetByIdSpec(recordsetKey))
            .FirstOrDefaultAsync(cancellationToken);

        if (recordset is null)
        {
            return null;
        }

        var columns = await dbContext.RecordsetColumns
            .AsNoTracking()
            .ApplySpecification(new RecordsetColumnsByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        var statuses = await dbContext.RecordsetStatuses
            .AsNoTracking()
            .ApplySpecification(new RecordsetStatusesByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        var transitions = await dbContext.RecordsetStatusTransitions
            .AsNoTracking()
            .ApplySpecification(new RecordsetStatusTransitionsByRecordsetIdSpec(recordsetKey))
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
            .ApplySpecification(new RecordsetByIdSpec(recordsetKey))
            .FirstOrDefaultAsync(cancellationToken);
        if (recordset is null)
        {
            return;
        }

        var items = await dbContext.RecordsetItems
            .ApplySpecification(new RecordsetItemsByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetItems.RemoveRange(items);

        var columns = await dbContext.RecordsetColumns
            .ApplySpecification(new RecordsetColumnsByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetColumns.RemoveRange(columns);

        var statuses = await dbContext.RecordsetStatuses
            .ApplySpecification(new RecordsetStatusesByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatuses.RemoveRange(statuses);

        var transitions = await dbContext.RecordsetStatusTransitions
            .ApplySpecification(new RecordsetStatusTransitionsByRecordsetIdSpec(recordsetKey))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatusTransitions.RemoveRange(transitions);

        var projections = await dbContext.RecordsetProjections
            .ApplySpecification(new RecordsetProjectionByRecordsetIdSpec(recordsetKey))
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
        pendingRecords.Add((record, entity));
    }

    public async Task UpdateRecordAsync(Record record, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RecordsetItems
            .ApplySpecification(new RecordsetItemByRecordsetIdAndIdSpec(record.RecordsetId.ToString(), record.Id))
            .FirstOrDefaultAsync(cancellationToken);

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
            .ApplySpecification(new RecordsetItemByRecordsetIdAndIdSpec(recordsetId.ToString(), recordId))
            .FirstOrDefaultAsync(cancellationToken);

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
        return dbContext.RecordsetItems
            .ApplySpecification(new RecordsetItemsByRecordsetIdSpec(recordsetKey))
            .CountAsync(cancellationToken);
    }

    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveAndAssignIdsAsync(false, cancellationToken);
    }

    Task<int> IUnitOfWork.SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken)
    {
        return SaveAndAssignIdsAsync(deferDispatch, cancellationToken);
    }

    private async Task<int> SaveAndAssignIdsAsync(bool deferDispatch, CancellationToken cancellationToken)
    {
        var result = await base.SaveChangesAsync(deferDispatch, cancellationToken);

        if (pendingRecords.Count == 0)
        {
            return result;
        }

        foreach (var (record, entity) in pendingRecords)
        {
            if (entity.Id > 0)
            {
                record.Id = (int)entity.Id;
            }
        }

        pendingRecords.Clear();
        return result;
    }

    private async Task ReplaceSchemaAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        var recordsetId = recordset.Id.ToString();

        var existingColumns = await dbContext.RecordsetColumns
            .ApplySpecification(new RecordsetColumnsByRecordsetIdSpec(recordsetId))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetColumns.RemoveRange(existingColumns);

        var existingStatuses = await dbContext.RecordsetStatuses
            .ApplySpecification(new RecordsetStatusesByRecordsetIdSpec(recordsetId))
            .ToListAsync(cancellationToken);
        dbContext.RecordsetStatuses.RemoveRange(existingStatuses);

        var existingTransitions = await dbContext.RecordsetStatusTransitions
            .ApplySpecification(new RecordsetStatusTransitionsByRecordsetIdSpec(recordsetId))
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
