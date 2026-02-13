using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record AllNotificationsRead(
    string UserId,
    DateTimeOffset ReadAt,
    DateTimeOffset? Before,
    UlidId? RecordsetId
) : INotification;
