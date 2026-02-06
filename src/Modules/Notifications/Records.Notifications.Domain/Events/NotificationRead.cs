using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationRead(
    UlidId NotificationId,
    string UserId,
    DateTimeOffset ReadAt
) : INotification;