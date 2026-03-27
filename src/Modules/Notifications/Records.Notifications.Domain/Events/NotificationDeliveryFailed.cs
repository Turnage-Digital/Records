using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationDeliveryFailed(
    UlidId NotificationId,
    DateTimeOffset FailedAt,
    string Reason,
    int AttemptNumber
) : INotification;