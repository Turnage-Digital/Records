using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationRead(
    UlidId NotificationId,
    DateTimeOffset ReadAt
) : INotification;