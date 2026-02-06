using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands.NotificationRules.Update;

public sealed record UpdateNotificationRuleCommand(
    UlidId RuleId,
    NotificationTrigger Trigger,
    NotificationChannelConfig[] Channels,
    NotificationSchedule Schedule,
    string? TemplateId,
    bool IsActive,
    string UpdatedBy,
    DateTimeOffset UpdatedAt
) : IRequest<NotificationRuleDto>;