using System.Text.Json;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.Mappers;

public static class NotificationRuleMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static NotificationRule ToDomain(NotificationRuleDb entity)
    {
        var trigger = JsonSerializer.Deserialize<NotificationTrigger>(entity.TriggerJson, SerializerOptions)
                      ?? throw new InvalidOperationException("Invalid trigger JSON");
        var channels = JsonSerializer.Deserialize<NotificationChannelConfig[]>(entity.ChannelsJson, SerializerOptions)
                       ?? [];
        var schedule = JsonSerializer.Deserialize<NotificationSchedule>(entity.ScheduleJson, SerializerOptions)
                       ?? NotificationSchedule.Immediate();

        return NotificationRule.Rehydrate(
            UlidId.Parse(entity.Id),
            UlidId.Parse(entity.TenantId),
            UlidId.Parse(entity.RecordsetId),
            entity.UserId,
            trigger,
            channels,
            schedule,
            entity.TemplateId,
            entity.IsActive,
            entity.IsDeleted
        );
    }

    public static NotificationRuleDb ToDb(NotificationRule rule)
    {
        return new NotificationRuleDb
        {
            Id = rule.Id.ToString(),
            TenantId = rule.TenantId.ToString(),
            RecordsetId = rule.RecordsetId.ToString(),
            UserId = rule.UserId,
            TriggerJson = JsonSerializer.Serialize(rule.Trigger, SerializerOptions),
            ChannelsJson = JsonSerializer.Serialize(rule.Channels, SerializerOptions),
            ScheduleJson = JsonSerializer.Serialize(rule.Schedule, SerializerOptions),
            TriggerType = (int)rule.Trigger.Type,
            TemplateId = rule.TemplateId,
            IsActive = rule.IsActive,
            IsDeleted = rule.IsDeleted
        };
    }

    public static void UpdateDb(NotificationRuleDb db, NotificationRule rule)
    {
        db.TriggerJson = JsonSerializer.Serialize(rule.Trigger, SerializerOptions);
        db.ChannelsJson = JsonSerializer.Serialize(rule.Channels, SerializerOptions);
        db.ScheduleJson = JsonSerializer.Serialize(rule.Schedule, SerializerOptions);
        db.TriggerType = (int)rule.Trigger.Type;
        db.TemplateId = rule.TemplateId;
        db.IsActive = rule.IsActive;
        db.IsDeleted = rule.IsDeleted;
    }
}
