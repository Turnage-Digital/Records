using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationCreated(
    UlidId NotificationId,
    UlidId TenantId,
    UlidId? RecordsetId,
    int? RecordId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor,
    string? RecipientUserId
) : INotification;