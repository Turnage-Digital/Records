using Records.Clocks.Domain;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql.Mappers;

public static class ClockMapper
{
    public static ClockDb ToDb(Clock clock)
    {
        return new ClockDb
        {
            Id = clock.Id.ToString(),
            TenantId = clock.TenantId.ToString(),
            RecordsetId = clock.RecordsetId.ToString(),
            RecordId = clock.RecordId,
            DefinitionId = clock.DefinitionId.ToString(),
            State = (int)clock.State,
            StartedAt = clock.StartedAt.UtcDateTime,
            AtRiskDueAt = clock.AtRiskDueAt.UtcDateTime,
            BreachDueAt = clock.BreachDueAt.UtcDateTime,
            AtRiskAt = clock.AtRiskAt?.UtcDateTime,
            BreachedAt = clock.BreachedAt?.UtcDateTime,
            PausedAt = clock.PausedAt?.UtcDateTime,
            CompletedAt = clock.CompletedAt?.UtcDateTime,
            PauseReason = clock.PauseReason,
            AccumulatedPauseMs = (long)clock.AccumulatedPauseTime.TotalMilliseconds
        };
    }

    public static Clock ToDomain(ClockDb entity)
    {
        return Clock.Rehydrate(
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