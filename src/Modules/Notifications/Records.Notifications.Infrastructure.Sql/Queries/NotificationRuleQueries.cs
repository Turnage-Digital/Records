using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql.Queries;

public sealed class NotificationRuleQueries(NotificationsDbContext context)
    : INotificationRuleQueries
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<NotificationRuleDto>> GetByListAsync(
        string userId,
        string? listId,
        CancellationToken cancellationToken
    )
    {
        var query = context.NotificationRules
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(listId))
        {
            query = query.Where(r => r.RecordsetId == listId);
        }

        var rules = await query.ToListAsync(cancellationToken);

        return rules.Select(rule =>
            {
                var trigger = JsonSerializer.Deserialize<NotificationTrigger>(rule.TriggerJson, SerializerOptions) ??
                              new NotificationTrigger { Type = 0 };
                var channels =
                    JsonSerializer.Deserialize<NotificationChannelConfig[]>(rule.ChannelsJson, SerializerOptions) ?? [];
                var schedule = JsonSerializer.Deserialize<NotificationSchedule>(rule.ScheduleJson, SerializerOptions) ??
                               NotificationSchedule.Immediate();

                return new NotificationRuleDto
                {
                    Id = rule.Id,
                    UserId = rule.UserId,
                    ListId = rule.RecordsetId,
                    IsActive = rule.IsActive,
                    TemplateId = rule.TemplateId,
                    Trigger = new NotificationTriggerDto
                    {
                        Type = trigger.Type,
                        FromValue = trigger.FromValue,
                        ToValue = trigger.ToValue,
                        ColumnName = trigger.ColumnName,
                        Operator = trigger.Operator,
                        Value = trigger.Value
                    },
                    Channels = channels
                        .Select(channel => new NotificationChannelDto
                        {
                            Type = channel.Type,
                            Address = channel.Address,
                            Settings = channel.Settings
                        })
                        .ToArray(),
                    Schedule = new NotificationScheduleDto
                    {
                        Type = schedule.Type,
                        Delay = schedule.Delay,
                        CronExpression = schedule.CronExpression,
                        DailyAt = schedule.DailyAt,
                        DaysOfWeek = schedule.DaysOfWeek
                    }
                };
            })
            .ToList();
    }
}