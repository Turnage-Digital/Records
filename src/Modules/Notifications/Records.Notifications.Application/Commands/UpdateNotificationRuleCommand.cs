using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands;

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

public sealed class UpdateNotificationRuleRequest
{
    public NotificationTrigger Trigger { get; set; } = null!;
    public NotificationChannelConfig[] Channels { get; set; } = [];
    public NotificationSchedule Schedule { get; set; } = null!;
    public string? TemplateId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateNotificationRuleCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<UpdateNotificationRuleCommand, NotificationRuleDto>
{
    public async Task<NotificationRuleDto> Handle(
        UpdateNotificationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var channels = request.Channels
            .Select(channel => new NotificationChannelConfig
            {
                Type = channel.Type,
                Address = channel.Address,
                Settings = new Dictionary<string, string>(channel.Settings)
            })
            .ToArray();

        var rule = await unitOfWork.NotificationRules.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null)
        {
            throw new InvalidOperationException("Notification rule not found.");
        }

        rule.Update(
            request.Trigger,
            channels,
            request.Schedule,
            request.TemplateId,
            request.IsActive,
            request.UpdatedBy,
            request.UpdatedAt
        );

        await unitOfWork.NotificationRules.UpdateAsync(rule, cancellationToken);
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
            TenantId = rule.TenantId.ToString(),
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