using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Contracts.Queries;

public interface INotificationRuleQueries
{
    Task<NotificationRuleDto?> GetByIdAsync(string ruleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationRuleDto>> GetByRecordsetAsync(
        string userId,
        string? recordsetId,
        CancellationToken cancellationToken
    );
}