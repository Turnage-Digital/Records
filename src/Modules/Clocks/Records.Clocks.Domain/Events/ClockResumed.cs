using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockResumed(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    DateTimeOffset ResumedAt,
    TimeSpan PauseDuration
) : INotification;