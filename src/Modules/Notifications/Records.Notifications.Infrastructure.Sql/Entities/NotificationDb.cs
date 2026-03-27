namespace Records.Notifications.Infrastructure.Sql.Entities;

public class NotificationDb
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string? RecordsetId { get; set; }
    public int? RecordId { get; set; }
    public string? NotificationRuleId { get; set; }
    public int TriggerType { get; set; }
    public int Channel { get; set; }
    public string RecipientAddress { get; set; } = null!;
    public string? RecipientDisplayName { get; set; }
    public string RecipientMetadataJson { get; set; } = "{}";
    public string? RecipientUserId { get; set; }
    public string ContentSubject { get; set; } = null!;
    public string ContentBody { get; set; } = null!;
    public string? ContentTemplateId { get; set; }
    public string ContentTemplateDataJson { get; set; } = "{}";
    public string ScheduleJson { get; set; } = "{}";
    public int Priority { get; set; }
    public int Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? CorrelationId { get; set; }

    public ICollection<DeliveryAttemptDb> DeliveryAttempts { get; set; } = new List<DeliveryAttemptDb>();

    public NotificationRuleDb? NotificationRule { get; set; }
}

public class DeliveryAttemptDb
{
    public int Id { get; set; }
    public string NotificationId { get; set; } = null!;
    public int Channel { get; set; }
    public int Status { get; set; }
    public DateTime AttemptedAt { get; set; }
    public int AttemptNumber { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? FailureReason { get; set; }
    public TimeSpan? NextRetryAfter { get; set; }

    public NotificationDb Notification { get; set; } = null!;
}