using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Contracts;

public interface INotificationRuleQueries
{
    Task<IReadOnlyList<NotificationRuleDto>> GetByListAsync(
        string userId,
        string? listId,
        CancellationToken cancellationToken
    );
}