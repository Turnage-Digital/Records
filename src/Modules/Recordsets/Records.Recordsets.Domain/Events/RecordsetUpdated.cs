using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Events;

public sealed record RecordsetUpdated(
    UlidId RecordsetId,
    DateTimeOffset OccurredAt
) : INotification;
