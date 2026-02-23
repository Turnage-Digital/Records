using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.QueryCriteria;

public sealed class PendingNotificationsCriteria : QueryCriteria<NotificationDb>
{
    public PendingNotificationsCriteria(DateTime asOfUtc, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Pending &&
            (n.ScheduledFor == null || n.ScheduledFor <= asOfUtc));

        ApplyOrderBy(n => n.CreatedAt);
        ApplyPaging(0, limit);
        AddInclude(n => n.DeliveryAttempts);
    }
}