using MediatR;
using Records.Notifications.Contracts;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Events;

namespace Records.Notifications.Application.EventHandlers;

public sealed class NotificationProjectionHandler(
    INotificationProjectionWriter writer
) : INotificationHandler<NotificationCreated>,
    INotificationHandler<NotificationQueued>,
    INotificationHandler<NotificationDelivered>,
    INotificationHandler<NotificationDeliveryFailed>,
    INotificationHandler<NotificationBounced>,
    INotificationHandler<NotificationCancelled>,
    INotificationHandler<NotificationRead>
{
    public Task Handle(NotificationBounced notification, CancellationToken cancellationToken)
    {
        return writer.UpdateBouncedAsync(
            notification.NotificationId.ToString(),
            notification.BouncedAt,
            notification.Reason,
            cancellationToken);
    }

    public Task Handle(NotificationCancelled notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCancelledAsync(
            notification.NotificationId.ToString(),
            notification.CancelledAt,
            notification.Reason,
            cancellationToken);
    }

    public Task Handle(NotificationCreated notification, CancellationToken cancellationToken)
    {
        var model = new NotificationProjectionModel(
            notification.NotificationId.ToString(),
            notification.TenantId.ToString(),
            notification.RecordsetId?.ToString(),
            notification.RecordId,
            notification.TriggerType,
            notification.Channel,
            DeliveryStatus.Pending,
            notification.RecipientUserId,
            notification.CreatedAt,
            notification.ScheduledFor
        );

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(NotificationDelivered notification, CancellationToken cancellationToken)
    {
        return writer.UpdateDeliveredAsync(
            notification.NotificationId.ToString(),
            notification.DeliveredAt,
            notification.ProviderMessageId,
            cancellationToken);
    }

    public Task Handle(NotificationDeliveryFailed notification, CancellationToken cancellationToken)
    {
        return writer.UpdateFailedAsync(
            notification.NotificationId.ToString(),
            notification.FailedAt,
            notification.Reason,
            notification.AttemptNumber,
            cancellationToken);
    }

    public Task Handle(NotificationQueued notification, CancellationToken cancellationToken)
    {
        return writer.UpdateQueuedAsync(
            notification.NotificationId.ToString(),
            notification.QueuedAt,
            cancellationToken);
    }

    public Task Handle(NotificationRead notification, CancellationToken cancellationToken)
    {
        return writer.UpdateReadAsync(
            notification.NotificationId.ToString(),
            notification.ReadAt,
            cancellationToken);
    }
}