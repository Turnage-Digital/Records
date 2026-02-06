using Records.Core.Domain.Specifications;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.Specifications;

public sealed class PendingNotificationsSpec : Specification<NotificationDb>
{
    public PendingNotificationsSpec(DateTime asOfUtc, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Pending &&
            (n.ScheduledFor == null || n.ScheduledFor <= asOfUtc));

        ApplyOrderBy(n => n.CreatedAt);
        ApplyPaging(0, limit);
        AddInclude(n => n.DeliveryAttempts);
    }
}