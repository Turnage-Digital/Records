using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationQueued(
    UlidId NotificationId,
    DateTimeOffset QueuedAt
) : INotification;