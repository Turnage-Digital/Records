using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.QueryCriteria;

public sealed class FailedNotificationsForRetryCriteria : QueryCriteria<NotificationDb>
{
    public FailedNotificationsForRetryCriteria(int maxAttempts, DateTime lastAttemptBefore, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Failed &&
            n.DeliveryAttempts.Count < maxAttempts &&
            n.DeliveryAttempts.Max(a => a.AttemptedAt) < lastAttemptBefore);

        ApplyOrderBy(n => n.Id);
        ApplyPaging(0, limit);
        AddInclude(n => n.DeliveryAttempts);
    }
}
