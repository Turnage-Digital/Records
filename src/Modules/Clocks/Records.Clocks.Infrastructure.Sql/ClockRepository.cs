using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Clocks.Infrastructure.Sql.QueryCriteria;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;

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
        var spec = new ClockByRecordAndDefinitionCriteria(
            recordsetId.ToString(),
            recordId,
            definitionId.ToString());
        var entity = await context.Clocks
            .AsNoTracking()
            .ApplyCriteria(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Clock>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClocksByRecordCriteria(recordsetId.ToString(), recordId);
        var entities = await context.Clocks
            .AsNoTracking()
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastThresholdCriteria(asOf.UtcDateTime);
        var entities = await context.Clocks
            .AsNoTracking()
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastDeadlineCriteria(asOf.UtcDateTime);
        var entities = await context.Clocks
            .AsNoTracking()
            .ApplyCriteria(spec)
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