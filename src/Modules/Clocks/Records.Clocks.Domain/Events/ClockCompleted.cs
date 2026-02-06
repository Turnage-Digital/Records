using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockCompleted(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    DateTimeOffset CompletedAt,
    DateTimeOffset BreachDueAt,
    bool WasAtRisk,
    TimeSpan TotalElapsed
) : INotification;