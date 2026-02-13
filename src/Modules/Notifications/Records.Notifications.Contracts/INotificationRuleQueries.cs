using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Contracts;

public interface INotificationRuleQueries
{
    Task<IReadOnlyList<NotificationRuleDto>> GetByRecordsetAsync(
        string userId,
        string? recordsetId,
        CancellationToken cancellationToken
    );
}