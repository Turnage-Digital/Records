using Records.Core.Application;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands;

public sealed record CreateNotificationCommand(
    NotificationTrigger Trigger,
    NotificationRecipient Recipient,
    NotificationContent Content,
    NotificationSchedule? Schedule = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? CorrelationId = null
) : RequestBase<Result<CreateNotificationResult>>;

public sealed record CreateNotificationResult(
    UlidId NotificationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);