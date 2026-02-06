using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationDelivered(
    UlidId NotificationId,
    UlidId TenantId,
    UlidId? RecordsetId,
    NotificationChannel Channel,
    DateTimeOffset DeliveredAt,
    string? ProviderMessageId
) : INotification;