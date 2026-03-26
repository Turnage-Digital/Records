namespace Records.Notifications.Infrastructure.Sql.Entities;

public class NotificationRuleDb
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string RecordsetId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string TriggerJson { get; set; } = "{}";
    public string ChannelsJson { get; set; } = "[]";
    public string ScheduleJson { get; set; } = "{}";
    public int TriggerType { get; set; }
    public string? TemplateId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<NotificationDb> Notifications { get; set; } = new List<NotificationDb>();
}
