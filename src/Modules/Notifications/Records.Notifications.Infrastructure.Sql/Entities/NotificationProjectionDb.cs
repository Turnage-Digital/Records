namespace Records.Notifications.Infrastructure.Sql.Entities;

public class NotificationProjectionDb
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string? RecordsetId { get; set; }
    public int? RecordId { get; set; }
    public int TriggerType { get; set; }
    public int Channel { get; set; }
    public int Status { get; set; }
    public string? RecipientUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastFailureReason { get; set; }
    public string? ProviderMessageId { get; set; }
}