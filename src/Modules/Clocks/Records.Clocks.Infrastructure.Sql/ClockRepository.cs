using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Clocks.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.Specifications;

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
        var spec = new ClockByRecordAndDefinitionSpec(
            recordsetId.ToString(),
            recordId,
            definitionId.ToString());
        var query = context.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Clock>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClocksByRecordSpec(recordsetId.ToString(), recordId);
        var query = context.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastThresholdSpec(asOf.UtcDateTime);
        var query = context.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

        return entities.Select(ClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Clock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastDeadlineSpec(asOf.UtcDateTime);
        var query = context.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

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
