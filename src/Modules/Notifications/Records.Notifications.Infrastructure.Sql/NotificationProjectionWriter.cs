using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Records.Notifications.Contracts;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class NotificationProjectionWriter(
    NotificationsDbContext dbContext,
    ILogger<NotificationProjectionWriter> logger
) : INotificationProjectionWriter
{
    public async Task UpsertAsync(NotificationProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

        if (record is null)
        {
            record = new NotificationProjectionDb
            {
                Id = model.Id,
                TenantId = model.TenantId,
                RecordsetId = model.RecordsetId,
                RecordId = model.RecordId,
                TriggerType = (int)model.TriggerType,
                Channel = (int)model.Channel,
                Status = (int)model.Status,
                RecipientUserId = model.RecipientUserId,
                CreatedAt = model.CreatedAt.UtcDateTime,
                ScheduledFor = model.ScheduledFor?.UtcDateTime
            };

            dbContext.NotificationProjections.Add(record);
        }
        else
        {
            record.TenantId = model.TenantId;
            record.RecordsetId = model.RecordsetId;
            record.RecordId = model.RecordId;
            record.TriggerType = (int)model.TriggerType;
            record.Channel = (int)model.Channel;
            record.Status = (int)model.Status;
            record.RecipientUserId = model.RecipientUserId;
            record.CreatedAt = model.CreatedAt.UtcDateTime;
            record.ScheduledFor = model.ScheduledFor?.UtcDateTime;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("NotificationProjection upserted: {NotificationId}", model.Id);
    }

    public Task UpdateQueuedAsync(string notificationId, DateTimeOffset queuedAt, CancellationToken cancellationToken)
    {
        return UpdateProjectionAsync(
            notificationId,
            record =>
            {
                record.Status = (int)DeliveryStatus.Queued;
                record.QueuedAt = queuedAt.UtcDateTime;
            },
            cancellationToken
        );
    }

    public Task UpdateDeliveredAsync(
        string notificationId,
        DateTimeOffset deliveredAt,
        string? providerMessageId,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            notificationId,
            record =>
            {
                record.Status = (int)DeliveryStatus.Delivered;
                record.DeliveredAt = deliveredAt.UtcDateTime;
                record.ProviderMessageId = providerMessageId;
            },
            cancellationToken
        );
    }

    public Task UpdateFailedAsync(
        string notificationId,
        DateTimeOffset failedAt,
        string reason,
        int attemptCount,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            notificationId,
            record =>
            {
                record.Status = (int)DeliveryStatus.Failed;
                record.FailedAt = failedAt.UtcDateTime;
                record.LastFailureReason = reason;
                record.AttemptCount = attemptCount;
            },
            cancellationToken
        );
    }

    public Task UpdateBouncedAsync(
        string notificationId,
        DateTimeOffset bouncedAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            notificationId,
            record =>
            {
                record.Status = (int)DeliveryStatus.Bounced;
                record.BouncedAt = bouncedAt.UtcDateTime;
                record.LastFailureReason = reason;
            },
            cancellationToken
        );
    }

    public Task UpdateCancelledAsync(
        string notificationId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            notificationId,
            record =>
            {
                record.Status = (int)DeliveryStatus.Cancelled;
                record.CancelledAt = cancelledAt.UtcDateTime;
                record.LastFailureReason = reason;
            },
            cancellationToken
        );
    }

    public Task UpdateReadAsync(string notificationId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        return UpdateProjectionAsync(
            notificationId,
            record => { record.ReadAt = readAt.UtcDateTime; },
            cancellationToken
        );
    }

    private async Task UpdateProjectionAsync(
        string notificationId,
        Action<NotificationProjectionDb> update,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            return;
        }

        update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("NotificationProjection updated: {NotificationId}", notificationId);
    }
}