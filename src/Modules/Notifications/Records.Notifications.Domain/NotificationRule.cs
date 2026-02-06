using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Domain;

public sealed class NotificationRule : AggregateRoot
{
    private NotificationRule()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId TenantId { get; private set; }
    public UlidId RecordsetId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public NotificationTrigger Trigger { get; private set; } = null!;
    public IReadOnlyList<NotificationChannelConfig> Channels { get; private set; } = [];
    public NotificationSchedule Schedule { get; private set; } = null!;
    public string? TemplateId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static NotificationRule Create(
        UlidId tenantId,
        UlidId recordsetId,
        string userId,
        NotificationTrigger trigger,
        IReadOnlyList<NotificationChannelConfig> channels,
        NotificationSchedule schedule,
        string? templateId,
        bool isActive,
        DateTimeOffset createdAt
    )
    {
        var rule = new NotificationRule
        {
            Id = UlidId.NewUlid(),
            TenantId = tenantId,
            RecordsetId = recordsetId,
            UserId = userId,
            Trigger = trigger,
            Channels = channels,
            Schedule = schedule,
            TemplateId = templateId,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = createdAt,
            CreatedBy = userId
        };

        return rule;
    }

    public static NotificationRule Rehydrate(
        UlidId id,
        UlidId tenantId,
        UlidId recordsetId,
        string userId,
        NotificationTrigger trigger,
        IReadOnlyList<NotificationChannelConfig> channels,
        NotificationSchedule schedule,
        string? templateId,
        bool isActive,
        bool isDeleted,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset? updatedAt,
        string? updatedBy
    )
    {
        return new NotificationRule
        {
            Id = id,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            UserId = userId,
            Trigger = trigger,
            Channels = channels,
            Schedule = schedule,
            TemplateId = templateId,
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };
    }

    public void Update(
        NotificationTrigger trigger,
        IReadOnlyList<NotificationChannelConfig> channels,
        NotificationSchedule schedule,
        string? templateId,
        bool isActive,
        string updatedBy,
        DateTimeOffset updatedAt
    )
    {
        Trigger = trigger;
        Channels = channels;
        Schedule = schedule;
        TemplateId = templateId;
        IsActive = isActive;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public void Delete(string deletedBy, DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = deletedAt;
    }

    public override string GetStreamId()
    {
        return $"NotificationRule:{Id}";
    }

    public override string GetStreamType()
    {
        return "NotificationRule";
    }
}