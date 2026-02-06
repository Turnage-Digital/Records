using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationBounced(
    UlidId NotificationId,
    DateTimeOffset BouncedAt,
    string Reason
) : INotification;