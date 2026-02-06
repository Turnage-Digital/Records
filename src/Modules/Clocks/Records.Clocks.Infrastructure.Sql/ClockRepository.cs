using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockRepository(ClocksDbContext context) : IClockRepository
{
    public async Task<Clock?> GetByIdAsync(UlidId clockId, CancellationToken cancellationToken)
    {
        var entity = await context.Clocks
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clockId.ToString(), cancellationToken);

        return entity is null ? null : ClockMapper.ToDomain(entity);
    }

    public async Task<Clock?> GetByRecordAndDefinitionAsync(
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        CancellationToken cancellationToken
    )
    {
        var entity = await context.Clocks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.RecordsetId == recordsetId.ToString() &&
                     c.RecordId == recordId &&
                     c.DefinitionId == definitionId.ToString(),
                cancellationToken);

        return entity is null ? null : ClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Clock>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        var entities = await context.Clocks
            .AsNoTracking()
            .Where(c => c.RecordsetId == recordsetId.ToString() && c.RecordId == recordId)
            .OrderBy(c => c.StartedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var entities = await context.Clocks
            .AsNoTracking()
            .Where(c =>
                c.State != (int)ClockState.Breached &&
                c.State != (int)ClockState.Completed &&
                c.AtRiskAt == null &&
                c.AtRiskDueAt <= asOf.UtcDateTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var entities = await context.Clocks
            .AsNoTracking()
            .Where(c =>
                c.State != (int)ClockState.Breached &&
                c.State != (int)ClockState.Completed &&
                c.BreachedAt == null &&
                c.BreachDueAt <= asOf.UtcDateTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public Task AddAsync(Clock clock, CancellationToken cancellationToken)
    {
        context.Clocks.Add(ClockMapper.ToDb(clock));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Clock clock, CancellationToken cancellationToken)
    {
        context.Clocks.Update(ClockMapper.ToDb(clock));
        return Task.CompletedTask;
    }
}