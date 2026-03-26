using System.Text.Json;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql.Mappers;

public static class NotificationMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Notification ToDomain(NotificationDb entity)
    {
        var recipientMetadata = DeserializeDictionary<string>(entity.RecipientMetadataJson);
        var templateData = DeserializeDictionary<object>(entity.ContentTemplateDataJson);
        var schedule = DeserializeSchedule(entity.ScheduleJson);

        var recipient = new NotificationRecipient
        {
            Channel = (NotificationChannel)entity.Channel,
            Address = entity.RecipientAddress,
            DisplayName = entity.RecipientDisplayName,
            UserId = entity.RecipientUserId,
            Metadata = recipientMetadata
        };

        var content = new NotificationContent
        {
            Subject = entity.ContentSubject,
            Body = entity.ContentBody,
            TemplateId = entity.ContentTemplateId,
            TemplateData = templateData
        };

        var attempts = entity.DeliveryAttempts.Select(ToDomain).ToList();

        return Notification.Rehydrate(
            UlidId.Parse(entity.Id),
            UlidId.Parse(entity.TenantId),
            entity.RecordsetId is null ? null : UlidId.Parse(entity.RecordsetId),
            entity.RecordId,
            entity.NotificationRuleId is null ? null : UlidId.Parse(entity.NotificationRuleId),
            (NotificationTriggerType)entity.TriggerType,
            recipient,
            content,
            schedule,
            (NotificationPriority)entity.Priority,
            (DeliveryStatus)entity.Status,
            entity.ScheduledFor.HasValue ? new DateTimeOffset(entity.ScheduledFor.Value, TimeSpan.Zero) : null,
            entity.ReadAt.HasValue ? new DateTimeOffset(entity.ReadAt.Value, TimeSpan.Zero) : null,
            entity.CorrelationId,
            attempts
        );
    }

    public static NotificationDb ToDb(Notification notification)
    {
        return new NotificationDb
        {
            Id = notification.Id.ToString(),
            TenantId = notification.TenantId.ToString(),
            RecordsetId = notification.RecordsetId?.ToString(),
            RecordId = notification.RecordId,
            NotificationRuleId = notification.NotificationRuleId?.ToString(),
            TriggerType = (int)notification.TriggerType,
            Channel = (int)notification.Recipient.Channel,
            RecipientAddress = notification.Recipient.Address,
            RecipientDisplayName = notification.Recipient.DisplayName,
            RecipientMetadataJson = Serialize(notification.Recipient.Metadata),
            RecipientUserId = notification.Recipient.UserId,
            ContentSubject = notification.Content.Subject,
            ContentBody = notification.Content.Body,
            ContentTemplateId = notification.Content.TemplateId,
            ContentTemplateDataJson = Serialize(notification.Content.TemplateData),
            ScheduleJson = Serialize(notification.Schedule),
            Priority = (int)notification.Priority,
            Status = (int)notification.Status,
            ScheduledFor = notification.ScheduledFor?.UtcDateTime,
            ReadAt = notification.ReadAt?.UtcDateTime,
            CorrelationId = notification.CorrelationId,
            DeliveryAttempts = notification.DeliveryAttempts
                .Select(attempt => ToDb(attempt, notification.Id.ToString()))
                .ToList()
        };
    }

    public static void UpdateDb(NotificationDb db, Notification notification)
    {
        db.RecordsetId = notification.RecordsetId?.ToString();
        db.RecordId = notification.RecordId;
        db.NotificationRuleId = notification.NotificationRuleId?.ToString();
        db.TriggerType = (int)notification.TriggerType;
        db.Channel = (int)notification.Recipient.Channel;
        db.RecipientAddress = notification.Recipient.Address;
        db.RecipientDisplayName = notification.Recipient.DisplayName;
        db.RecipientMetadataJson = Serialize(notification.Recipient.Metadata);
        db.RecipientUserId = notification.Recipient.UserId;
        db.ContentSubject = notification.Content.Subject;
        db.ContentBody = notification.Content.Body;
        db.ContentTemplateId = notification.Content.TemplateId;
        db.ContentTemplateDataJson = Serialize(notification.Content.TemplateData);
        db.ScheduleJson = Serialize(notification.Schedule);
        db.Priority = (int)notification.Priority;
        db.Status = (int)notification.Status;
        db.ScheduledFor = notification.ScheduledFor?.UtcDateTime;
        db.ReadAt = notification.ReadAt?.UtcDateTime;
        db.CorrelationId = notification.CorrelationId;

        db.DeliveryAttempts.Clear();
        foreach (var attempt in notification.DeliveryAttempts)
        {
            db.DeliveryAttempts.Add(ToDb(attempt, notification.Id.ToString()));
        }
    }

    private static DeliveryAttempt ToDomain(DeliveryAttemptDb db)
    {
        return new DeliveryAttempt
        {
            Channel = (NotificationChannel)db.Channel,
            Status = (DeliveryStatus)db.Status,
            AttemptedAt = new DateTimeOffset(db.AttemptedAt, TimeSpan.Zero),
            AttemptNumber = db.AttemptNumber,
            ProviderMessageId = db.ProviderMessageId,
            FailureReason = db.FailureReason,
            NextRetryAfter = db.NextRetryAfter
        };
    }

    private static DeliveryAttemptDb ToDb(DeliveryAttempt attempt, string notificationId)
    {
        return new DeliveryAttemptDb
        {
            NotificationId = notificationId,
            Channel = (int)attempt.Channel,
            Status = (int)attempt.Status,
            AttemptedAt = attempt.AttemptedAt.UtcDateTime,
            AttemptNumber = attempt.AttemptNumber,
            ProviderMessageId = attempt.ProviderMessageId,
            FailureReason = attempt.FailureReason,
            NextRetryAfter = attempt.NextRetryAfter
        };
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    private static Dictionary<string, T> DeserializeDictionary<T>(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, T>>(json, SerializerOptions)
               ?? new Dictionary<string, T>();
    }

    private static NotificationSchedule DeserializeSchedule(string json)
    {
        return JsonSerializer.Deserialize<NotificationSchedule>(json, SerializerOptions)
               ?? NotificationSchedule.Immediate();
    }
}