using Records.Core.Domain;

namespace Records.Notifications.Domain;

public interface INotificationsUnitOfWork : IUnitOfWork
{
    INotificationRepository Notifications { get; }
    INotificationRuleRepository NotificationRules { get; }
}