using Microsoft.EntityFrameworkCore;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Clocks.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.Specifications;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockQueries(ClocksDbContext dbContext) : IClockQueries
{
    public async Task<ClockDto?> GetByIdAsync(UlidId clockId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Clocks
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clockId.ToString(), cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<ClockDto?> GetByRecordAndDefinitionAsync(
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
        var query = dbContext.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<ClockDto>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClocksByRecordSpec(recordsetId.ToString(), recordId);
        var query = dbContext.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ClockWatchdogDto>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastThresholdSpec(asOf.UtcDateTime);
        var query = dbContext.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        return await query
            .Select(c => new ClockWatchdogDto(
                c.Id,
                c.RecordsetId,
                c.RecordId,
                c.TenantId,
                c.DefinitionId,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.BreachDueAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskDueAt, TimeSpan.Zero)
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClockWatchdogDto>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastDeadlineSpec(asOf.UtcDateTime);
        var query = dbContext.Clocks
            .AsNoTracking()
            .ApplySpecification(spec);
        return await query
            .Select(c => new ClockWatchdogDto(
                c.Id,
                c.RecordsetId,
                c.RecordId,
                c.TenantId,
                c.DefinitionId,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.BreachDueAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskDueAt, TimeSpan.Zero)
            ))
            .ToListAsync(cancellationToken);
    }

    private static ClockDto Map(ClockDb entity)
    {
        return new ClockDto(
            UlidId.Parse(entity.Id),
            UlidId.Parse(entity.TenantId),
            UlidId.Parse(entity.RecordsetId),
            entity.RecordId,
            UlidId.Parse(entity.DefinitionId),
            (ClockState)entity.State,
            new DateTimeOffset(entity.StartedAt, TimeSpan.Zero),
            new DateTimeOffset(entity.AtRiskDueAt, TimeSpan.Zero),
            new DateTimeOffset(entity.BreachDueAt, TimeSpan.Zero),
            entity.AtRiskAt.HasValue ? new DateTimeOffset(entity.AtRiskAt.Value, TimeSpan.Zero) : null,
            entity.BreachedAt.HasValue ? new DateTimeOffset(entity.BreachedAt.Value, TimeSpan.Zero) : null,
            entity.PausedAt.HasValue ? new DateTimeOffset(entity.PausedAt.Value, TimeSpan.Zero) : null,
            entity.CompletedAt.HasValue ? new DateTimeOffset(entity.CompletedAt.Value, TimeSpan.Zero) : null,
            entity.PauseReason,
            TimeSpan.FromMilliseconds(entity.AccumulatedPauseMs)
        );
    }
}
