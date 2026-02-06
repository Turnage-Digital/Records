using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands.NotificationRules.Update;

public sealed class UpdateNotificationRuleRequest
{
    public NotificationTrigger Trigger { get; set; } = null!;
    public NotificationChannelConfig[] Channels { get; set; } = [];
    public NotificationSchedule Schedule { get; set; } = null!;
    public string? TemplateId { get; set; }
    public bool IsActive { get; set; } = true;
    public string UserId { get; set; } = string.Empty;
}