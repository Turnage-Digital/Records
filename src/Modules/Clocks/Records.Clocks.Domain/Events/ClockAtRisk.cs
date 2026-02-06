using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockAtRisk(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    DateTimeOffset AtRiskAt,
    DateTimeOffset BreachDueAt
) : INotification;