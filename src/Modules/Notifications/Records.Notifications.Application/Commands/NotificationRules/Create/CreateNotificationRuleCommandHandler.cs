using MediatR;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands.NotificationRules.Create;

public sealed class CreateNotificationRuleCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<CreateNotificationRuleCommand, NotificationRuleDto>
{
    public async Task<NotificationRuleDto> Handle(
        CreateNotificationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var rule = NotificationRule.Create(
            request.TenantId,
            request.RecordsetId,
            request.UserId,
            request.Trigger,
            request.Channels,
            request.Schedule,
            request.TemplateId,
            request.IsActive,
            request.CreatedAt
        );

        await unitOfWork.NotificationRules.AddAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(rule);
    }

    private static NotificationRuleDto Map(NotificationRule rule)
    {
        return new NotificationRuleDto
        {
            Id = rule.Id.ToString(),
            UserId = rule.UserId,
            RecordsetId = rule.RecordsetId.ToString(),
            IsActive = rule.IsActive,
            TemplateId = rule.TemplateId,
            Trigger = new NotificationTriggerDto
            {
                Type = rule.Trigger.Type,
                FromValue = rule.Trigger.FromValue,
                ToValue = rule.Trigger.ToValue,
                ColumnName = rule.Trigger.ColumnName,
                Operator = rule.Trigger.Operator,
                Value = rule.Trigger.Value
            },
            Channels = rule.Channels
                .Select(channel => new NotificationChannelDto
                {
                    Type = channel.Type,
                    Address = channel.Address,
                    Settings = channel.Settings
                })
                .ToArray(),
            Schedule = new NotificationScheduleDto
            {
                Type = rule.Schedule.Type,
                Delay = rule.Schedule.Delay,
                CronExpression = rule.Schedule.CronExpression,
                DailyAt = rule.Schedule.DailyAt,
                DaysOfWeek = rule.Schedule.DaysOfWeek
            }
        };
    }
}