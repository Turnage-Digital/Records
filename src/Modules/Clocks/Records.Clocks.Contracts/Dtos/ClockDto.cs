using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Dtos;

public sealed record ClockDto(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset AtRiskDueAt,
    DateTimeOffset BreachDueAt,
    DateTimeOffset? AtRiskAt,
    DateTimeOffset? BreachedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    string? PauseReason,
    TimeSpan AccumulatedPauseTime
);