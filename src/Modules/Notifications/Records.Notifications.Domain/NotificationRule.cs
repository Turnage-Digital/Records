using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain.Events;
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

    public static NotificationRule Create(
        UlidId tenantId,
        UlidId recordsetId,
        string userId,
        NotificationTrigger trigger,
        IReadOnlyList<NotificationChannelConfig> channels,
        NotificationSchedule schedule,
        string? templateId,
        bool isActive
    )
    {
        var rule = new NotificationRule
        {
            Id = UlidId.NewUlid(),
            TenantId = tenantId,
            RecordsetId = recordsetId,
            UserId = userId,
            Trigger = trigger,
            Channels = channels.ToArray(),
            Schedule = schedule,
            TemplateId = templateId,
            IsActive = isActive,
            IsDeleted = false
        };

        rule.AddDomainEvent(new NotificationRuleCreated(
            rule.Id,
            rule.TenantId,
            rule.RecordsetId,
            rule.UserId,
            rule.Trigger,
            rule.Channels.ToArray(),
            rule.Schedule,
            rule.TemplateId,
            rule.IsActive));

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
        bool isDeleted
    )
    {
        return new NotificationRule
        {
            Id = id,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            UserId = userId,
            Trigger = trigger,
            Channels = channels.ToArray(),
            Schedule = schedule,
            TemplateId = templateId,
            IsActive = isActive,
            IsDeleted = isDeleted
        };
    }

    public void Update(
        NotificationTrigger trigger,
        IReadOnlyList<NotificationChannelConfig> channels,
        NotificationSchedule schedule,
        string? templateId,
        bool isActive
    )
    {
        Trigger = trigger;
        Channels = channels.ToArray();
        Schedule = schedule;
        TemplateId = templateId;
        IsActive = isActive;

        AddDomainEvent(new NotificationRuleUpdated(
            Id,
            TenantId,
            RecordsetId,
            UserId,
            Trigger,
            Channels.ToArray(),
            Schedule,
            TemplateId,
            IsActive));
    }

    public void Delete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;

        AddDomainEvent(new NotificationRuleDeleted(Id, TenantId));
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
