using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands.NotificationRules.Create;

public sealed class CreateNotificationRuleRequest
{
    public string RecordsetId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public NotificationTrigger Trigger { get; set; } = null!;
    public NotificationChannelConfig[] Channels { get; set; } = [];
    public NotificationSchedule Schedule { get; set; } = null!;
    public string? TemplateId { get; set; }
    public bool IsActive { get; set; } = true;
}