using Records.Notifications.Domain;

namespace Records.Notifications.Contracts.Projections;

public interface INotificationProjectionWriter
{
    Task UpsertAsync(NotificationProjectionModel model, CancellationToken cancellationToken);
    Task UpdateQueuedAsync(string notificationId, DateTimeOffset queuedAt, CancellationToken cancellationToken);

    Task UpdateDeliveredAsync(
        string notificationId,
        DateTimeOffset deliveredAt,
        string? providerMessageId,
        CancellationToken cancellationToken
    );

    Task UpdateFailedAsync(
        string notificationId,
        DateTimeOffset failedAt,
        string reason,
        int attemptCount,
        CancellationToken cancellationToken
    );

    Task UpdateBouncedAsync(
        string notificationId,
        DateTimeOffset bouncedAt,
        string reason,
        CancellationToken cancellationToken
    );

    Task UpdateCancelledAsync(
        string notificationId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken
    );

    Task UpdateReadAsync(
        string notificationId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken
    );
}

public sealed record NotificationProjectionModel(
    string Id,
    string TenantId,
    string? RecordsetId,
    int? RecordId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    DeliveryStatus Status,
    string? RecipientUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);