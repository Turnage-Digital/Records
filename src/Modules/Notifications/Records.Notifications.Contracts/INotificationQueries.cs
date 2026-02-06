using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Contracts;

public interface INotificationQueries
{
    Task<NotificationDetailsDto?> GetByIdAsync(
        string notificationId,
        string userId,
        CancellationToken cancellationToken
    );

    Task<NotificationListPageDto> GetPageAsync(
        string userId,
        string? listId,
        DateTimeOffset? since,
        bool? unread,
        int pageSize,
        int page,
        CancellationToken cancellationToken
    );

    Task<int> GetUnreadCountAsync(string userId, string? listId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPendingDto>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<NotificationPendingDto>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken
    );
}