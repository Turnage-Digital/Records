using Records.Clocks.Domain;

namespace Records.Clocks.Contracts.Dtos;

public sealed record ClockWatchdogDto(
    string ClockId,
    string RecordsetId,
    int RecordId,
    string TenantId,
    string DefinitionId,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset BreachDueAt,
    DateTimeOffset AtRiskDueAt
);