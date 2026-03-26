using MediatR;
using Records.Core.Contracts;
using Records.Core.Infrastructure.Sql;
using Records.Notifications.Domain;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class NotificationsUnitOfWork(
    NotificationsDbContext context,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<NotificationsDbContext>(context, mediator, eventStore, serializer, tenantContext),
        INotificationsUnitOfWork
{
    public INotificationRepository Notifications { get; } = new NotificationRepository(context);
    public INotificationRuleRepository NotificationRules { get; } = new NotificationRuleRepository(context);
}
