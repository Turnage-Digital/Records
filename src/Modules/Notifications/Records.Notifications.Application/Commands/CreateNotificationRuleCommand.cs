using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands;

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

public sealed class CreateNotificationRuleRequest
{
    public string RecordsetId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public NotificationTrigger Trigger { get; set; } = null!;
    public NotificationChannelConfig[] Channels { get; set; } = [];
    public NotificationSchedule Schedule { get; set; } = null!;
    public string? TemplateId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateNotificationRuleCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<CreateNotificationRuleCommand, NotificationRuleDto>
{
    public async Task<NotificationRuleDto> Handle(
        CreateNotificationRuleCommand request,
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

        var rule = NotificationRule.Create(
            request.TenantId,
            request.RecordsetId,
            request.UserId,
            request.Trigger,
            channels,
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