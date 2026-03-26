using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Mappers;
using Records.Notifications.Infrastructure.Sql.QueryCriteria;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var db = await context.Notifications
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == id.ToString(), cancellationToken);

        return db is null ? null : NotificationMapper.ToDomain(db);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new PendingNotificationsCriteria(DateTime.UtcNow, limit);

        var pending = await context.Notifications
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return pending.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Notification>> GetByRecordsetIdAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new NotificationsByRecordsetIdCriteria(recordsetId.ToString());

        var notifications = await context.Notifications
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return notifications.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var cutoff = DateTime.UtcNow.Subtract(retryAfter);
        var spec = new FailedNotificationsForRetryCriteria(maxAttempts, cutoff, limit);

        var failed = await context.Notifications
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return failed.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task MarkAllAsReadAsync(
        string userId,
        DateTimeOffset readAt,
        DateTimeOffset? before = null,
        UlidId? recordsetId = null,
        CancellationToken cancellationToken = default
    )
    {
        var projectionQuery = context.NotificationProjections
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null);

        if (before.HasValue)
        {
            projectionQuery = projectionQuery.Where(n => n.CreatedAt <= before.Value.UtcDateTime);
        }

        if (recordsetId.HasValue)
        {
            var recordsetKey = recordsetId.Value.ToString();
            projectionQuery = projectionQuery.Where(n => n.RecordsetId == recordsetKey);
        }

        var notificationIds = await projectionQuery
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        if (notificationIds.Count == 0)
        {
            return;
        }

        var notifications = await context.Notifications
            .Where(n => notificationIds.Contains(n.Id) && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.ReadAt = readAt.UtcDateTime;
        }
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var db = NotificationMapper.ToDb(notification);
        await context.Notifications.AddAsync(db, cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var db = await context.Notifications
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == notification.Id.ToString(), cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Notification '{notification.Id}' not found.");
        }

        NotificationMapper.UpdateDb(db, notification);
    }
}
