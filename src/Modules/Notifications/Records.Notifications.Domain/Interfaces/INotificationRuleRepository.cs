using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain;

public interface INotificationRuleRepository
{
    Task<NotificationRule?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRule>> ListByRecordsetAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<NotificationRule>> ListActiveByRecordsetAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(NotificationRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationRule rule, CancellationToken cancellationToken = default);
}