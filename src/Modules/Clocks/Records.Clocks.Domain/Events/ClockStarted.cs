using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockStarted(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    DateTimeOffset StartedAt,
    DateTimeOffset AtRiskDueAt,
    DateTimeOffset BreachDueAt
) : INotification;