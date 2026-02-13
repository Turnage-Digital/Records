using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByRecordsetIdAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task MarkAllAsReadAsync(
        string userId,
        DateTimeOffset readAt,
        DateTimeOffset? before = null,
        UlidId? recordsetId = null,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
