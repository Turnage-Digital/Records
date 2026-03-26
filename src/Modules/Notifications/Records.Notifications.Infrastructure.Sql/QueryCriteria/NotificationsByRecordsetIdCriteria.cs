using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.QueryCriteria;

public sealed class NotificationsByRecordsetIdCriteria : QueryCriteria<NotificationDb>
{
    public NotificationsByRecordsetIdCriteria(string recordsetId)
    {
        AddCriteria(n => n.RecordsetId == recordsetId);
        ApplyOrderByDescending(n => n.Id);
        AddInclude(n => n.DeliveryAttempts);
    }
}