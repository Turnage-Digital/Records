using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands.NotificationRules.Create;

public sealed record CreateNotificationRuleCommand(
    UlidId TenantId,
    UlidId RecordsetId,
    string UserId,
    NotificationTrigger Trigger,
    NotificationChannelConfig[] Channels,
    NotificationSchedule Schedule,
    string? TemplateId,
    bool IsActive,
    DateTimeOffset CreatedAt
) : IRequest<NotificationRuleDto>;