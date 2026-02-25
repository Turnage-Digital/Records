using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Records.Core.Contracts;
using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql.Entities;
using Records.Recordsets.Infrastructure.Sql.Mappers;
using Records.Recordsets.Infrastructure.Sql.QueryCriteria;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetsUnitOfWork : UnitOfWork<RecordsetsDbContext>, IRecordsetsUnitOfWork
{
    private readonly RecordsetsDbContext _dbContext;
    private readonly List<(Record Record, RecordDb Entity)> _pendingRecords = [];

    public RecordsetsUnitOfWork(
        RecordsetsDbContext dbContext,
        IMediator mediator,
        IEventStore? eventStore = null,
        IDomainEventSerializer? serializer = null,
        ITenantContext? tenantContext = null
    )
        : base(dbContext, mediator, eventStore, serializer, tenantContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await _dbContext.Recordsets
            .AsNoTracking()
            .ApplyCriteria(new RecordsetByIdCriteria(recordsetKey))
            .FirstOrDefaultAsync(cancellationToken);

        if (recordset is null)
        {
            return null;
        }

        var columns = await _dbContext.RecordsetColumns
            .AsNoTracking()
            .ApplyCriteria(new RecordsetColumnsByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.RecordsetStatuses
            .AsNoTracking()
            .ApplyCriteria(new RecordsetStatusesByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        var transitions = await _dbContext.RecordsetStatusTransitions
            .AsNoTracking()
            .ApplyCriteria(new RecordsetStatusTransitionsByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);

        return RecordsetMapper.ToDomain(recordset, columns, statuses, transitions);
    }

    public async Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var recordset = await _dbContext.Recordsets
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
        await _dbContext.Recordsets.AddAsync(recordsetDb, cancellationToken);

        await ReplaceSchemaAsync(recordset, cancellationToken);
    }

    public Task UpdateRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        _dbContext.Recordsets.Update(RecordsetMapper.ToDb(recordset));
        return ReplaceSchemaAsync(recordset, cancellationToken);
    }

    public async Task DeleteRecordsetAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await _dbContext.Recordsets
            .ApplyCriteria(new RecordsetByIdCriteria(recordsetKey))
            .FirstOrDefaultAsync(cancellationToken);
        if (recordset is null)
        {
            return;
        }

        var items = await _dbContext.RecordsetItems
            .ApplyCriteria(new RecordsetItemsByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetItems.RemoveRange(items);

        var columns = await _dbContext.RecordsetColumns
            .ApplyCriteria(new RecordsetColumnsByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetColumns.RemoveRange(columns);

        var statuses = await _dbContext.RecordsetStatuses
            .ApplyCriteria(new RecordsetStatusesByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetStatuses.RemoveRange(statuses);

        var transitions = await _dbContext.RecordsetStatusTransitions
            .ApplyCriteria(new RecordsetStatusTransitionsByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetStatusTransitions.RemoveRange(transitions);

        var projections = await _dbContext.RecordsetProjections
            .ApplyCriteria(new RecordsetProjectionByRecordsetIdCriteria(recordsetKey))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetProjections.RemoveRange(projections);

        _dbContext.Recordsets.Remove(recordset);
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
        await _dbContext.RecordsetItems.AddAsync(entity, cancellationToken);
        _pendingRecords.Add((record, entity));
    }

    public async Task UpdateRecordAsync(Record record, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RecordsetItems
            .ApplyCriteria(new RecordsetItemByRecordsetIdAndIdCriteria(record.RecordsetId.ToString(), record.Id))
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
        var item = await _dbContext.RecordsetItems
            .AsNoTracking()
            .ApplyCriteria(new RecordsetItemByRecordsetIdAndIdCriteria(recordsetId.ToString(), recordId))
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
        return _dbContext.RecordsetItems
            .ApplyCriteria(new RecordsetItemsByRecordsetIdCriteria(recordsetKey))
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

        if (_pendingRecords.Count == 0)
        {
            return result;
        }

        foreach (var (record, entity) in _pendingRecords)
        {
            if (entity.Id > 0)
            {
                record.Id = (int)entity.Id;
            }
        }

        _pendingRecords.Clear();
        return result;
    }

    private async Task ReplaceSchemaAsync(Recordset recordset, CancellationToken cancellationToken)
    {
        var recordsetId = recordset.Id.ToString();

        var existingColumns = await _dbContext.RecordsetColumns
            .ApplyCriteria(new RecordsetColumnsByRecordsetIdCriteria(recordsetId))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetColumns.RemoveRange(existingColumns);

        var existingStatuses = await _dbContext.RecordsetStatuses
            .ApplyCriteria(new RecordsetStatusesByRecordsetIdCriteria(recordsetId))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetStatuses.RemoveRange(existingStatuses);

        var existingTransitions = await _dbContext.RecordsetStatusTransitions
            .ApplyCriteria(new RecordsetStatusTransitionsByRecordsetIdCriteria(recordsetId))
            .ToListAsync(cancellationToken);
        _dbContext.RecordsetStatusTransitions.RemoveRange(existingTransitions);

        foreach (var column in recordset.Columns)
        {
            _dbContext.RecordsetColumns.Add(RecordsetMapper.ToDb(recordsetId, column));
        }

        foreach (var status in recordset.Statuses)
        {
            _dbContext.RecordsetStatuses.Add(RecordsetMapper.ToDb(recordsetId, status));
        }

        foreach (var transition in recordset.StatusTransitions)
        {
            _dbContext.RecordsetStatusTransitions.Add(RecordsetMapper.ToDb(recordsetId, transition));
        }
    }
}