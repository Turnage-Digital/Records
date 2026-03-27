using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.Events;

public sealed record NotificationRuleDeleted(
    UlidId NotificationRuleId,
    UlidId TenantId
) : INotification;