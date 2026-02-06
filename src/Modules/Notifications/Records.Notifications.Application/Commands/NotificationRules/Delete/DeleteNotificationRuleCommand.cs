using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands.NotificationRules.Delete;

public sealed record DeleteNotificationRuleCommand(
    UlidId RuleId,
    string DeletedBy,
    DateTimeOffset DeletedAt
) : IRequest;