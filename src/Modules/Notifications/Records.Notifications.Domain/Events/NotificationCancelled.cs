using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationCancelled(
    UlidId NotificationId,
    DateTimeOffset CancelledAt,
    string Reason
) : INotification;