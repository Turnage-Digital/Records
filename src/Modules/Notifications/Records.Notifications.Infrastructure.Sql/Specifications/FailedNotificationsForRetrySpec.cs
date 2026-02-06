using Records.Core.Domain.Specifications;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.Specifications;

public sealed class FailedNotificationsForRetrySpec : Specification<NotificationDb>
{
    public FailedNotificationsForRetrySpec(int maxAttempts, DateTime lastAttemptBefore, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Failed &&
            n.DeliveryAttempts.Count < maxAttempts &&
            n.DeliveryAttempts.Max(a => a.AttemptedAt) < lastAttemptBefore);

        ApplyOrderBy(n => n.CreatedAt);
        ApplyPaging(0, limit);
        AddInclude(n => n.DeliveryAttempts);
    }
}