using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Events;

public sealed record RecordsetCreated(
    UlidId RecordsetId,
    string Name,
    DateTimeOffset OccurredAt
) : INotification;
