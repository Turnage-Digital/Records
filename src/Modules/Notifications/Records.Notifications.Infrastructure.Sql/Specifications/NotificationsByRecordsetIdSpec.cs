using Records.Core.Domain.Specifications;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.Specifications;

public sealed class NotificationsByRecordsetIdSpec : Specification<NotificationDb>
{
    public NotificationsByRecordsetIdSpec(string recordsetId)
    {
        AddCriteria(n => n.RecordsetId == recordsetId);
        ApplyOrderByDescending(n => n.CreatedAt);
        AddInclude(n => n.DeliveryAttempts);
    }
}