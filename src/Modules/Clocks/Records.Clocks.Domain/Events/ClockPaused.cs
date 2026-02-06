using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockPaused(
    UlidId ClockId,
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    string Reason,
    DateTimeOffset PausedAt
) : INotification;