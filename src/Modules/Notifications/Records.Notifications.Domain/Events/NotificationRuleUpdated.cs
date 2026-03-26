using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationRuleUpdated(
    UlidId NotificationRuleId,
    UlidId TenantId,
    UlidId RecordsetId,
    string UserId,
    NotificationTrigger Trigger,
    NotificationChannelConfig[] Channels,
    NotificationSchedule Schedule,
    string? TemplateId,
    bool IsActive
) : INotification;
