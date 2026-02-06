using MediatR;
using Records.Core.Infrastructure.Sql;
using Records.Notifications.Domain;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class NotificationsUnitOfWork(
    NotificationsDbContext context,
    IMediator mediator
)
    : UnitOfWork<NotificationsDbContext>(context, mediator), INotificationsUnitOfWork
{
    public INotificationRepository Notifications { get; } = new NotificationRepository(context);
    public INotificationRuleRepository NotificationRules { get; } = new NotificationRuleRepository(context);
}