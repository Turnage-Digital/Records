using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain.Events;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Domain;

public sealed class Notification : AggregateRoot
{
    private readonly List<DeliveryAttempt> _deliveryAttempts = [];

    private Notification()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId TenantId { get; private set; }
    public UlidId? RecordsetId { get; private set; }
    public int? RecordId { get; private set; }
    public UlidId? NotificationRuleId { get; private set; }
    public NotificationTriggerType TriggerType { get; private set; }
    public NotificationRecipient Recipient { get; private set; } = null!;
    public NotificationContent Content { get; private set; } = null!;
    public NotificationSchedule Schedule { get; private set; } = null!;
    public NotificationPriority Priority { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public string? CorrelationId { get; private set; }
    public IReadOnlyList<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    public static Notification Rehydrate(
        UlidId id,
        UlidId tenantId,
        UlidId? recordsetId,
        int? recordId,
        UlidId? notificationRuleId,
        NotificationTriggerType triggerType,
        NotificationRecipient recipient,
        NotificationContent content,
        NotificationSchedule schedule,
        NotificationPriority priority,
        DeliveryStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? scheduledFor,
        DateTimeOffset? processedAt,
        DateTimeOffset? deliveredAt,
        DateTimeOffset? readAt,
        string? correlationId,
        IEnumerable<DeliveryAttempt> deliveryAttempts
    )
    {
        var notification = new Notification
        {
            Id = id,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            NotificationRuleId = notificationRuleId,
            TriggerType = triggerType,
            Recipient = recipient,
            Content = content,
            Schedule = schedule,
            Priority = priority,
            Status = status,
            CreatedAt = createdAt,
            ScheduledFor = scheduledFor,
            ProcessedAt = processedAt,
            DeliveredAt = deliveredAt,
            ReadAt = readAt,
            CorrelationId = correlationId
        };

        notification._deliveryAttempts.AddRange(deliveryAttempts);
        return notification;
    }

    public static Notification Create(
        NotificationTrigger trigger,
        NotificationRecipient recipient,
        NotificationContent content,
        NotificationSchedule? schedule,
        NotificationPriority priority,
        DateTimeOffset createdAt,
        string? correlationId = null,
        UlidId? notificationRuleId = null
    )
    {
        var effectiveSchedule = schedule ?? NotificationSchedule.Immediate();
        var scheduledFor = ComputeScheduledTime(effectiveSchedule, createdAt);

        var notification = new Notification
        {
            Id = UlidId.NewUlid(),
            TenantId = trigger.TenantId,
            RecordsetId = trigger.RecordsetId,
            RecordId = trigger.RecordId,
            NotificationRuleId = notificationRuleId,
            TriggerType = trigger.Type,
            Recipient = recipient,
            Content = content,
            Schedule = effectiveSchedule,
            Priority = priority,
            Status = DeliveryStatus.Pending,
            CreatedAt = createdAt,
            ScheduledFor = scheduledFor,
            CorrelationId = correlationId
        };

        notification.AddDomainEvent(new NotificationCreated(
            notification.Id,
            notification.TenantId,
            notification.RecordsetId,
            notification.RecordId,
            notification.TriggerType,
            notification.Recipient.Channel,
            createdAt,
            scheduledFor,
            notification.Recipient.UserId
        ));

        return notification;
    }

    public override string GetStreamId()
    {
        return $"Notification:{Id}";
    }

    public override string GetStreamType()
    {
        return "Notification";
    }

    public void MarkQueued(DateTimeOffset queuedAt)
    {
        EnsureCanQueue();

        Status = DeliveryStatus.Queued;
        ProcessedAt = queuedAt;

        AddDomainEvent(new NotificationQueued(Id, queuedAt));
    }

    public void RecordDeliverySuccess(DateTimeOffset deliveredAt, string? providerMessageId = null)
    {
        if (Status == DeliveryStatus.Delivered)
        {
            return;
        }

        var attempt = DeliveryAttempt.Success(
            Recipient.Channel,
            deliveredAt,
            _deliveryAttempts.Count + 1,
            providerMessageId);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Delivered;
        DeliveredAt = deliveredAt;

        AddDomainEvent(new NotificationDelivered(
            Id,
            TenantId,
            RecordsetId,
            Recipient.Channel,
            deliveredAt,
            providerMessageId));
    }

    public void RecordDeliveryFailure(DateTimeOffset failedAt, string reason, TimeSpan? retryAfter = null)
    {
        var attempt = DeliveryAttempt.Failure(
            Recipient.Channel,
            failedAt,
            _deliveryAttempts.Count + 1,
            reason,
            retryAfter);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Failed;

        AddDomainEvent(new NotificationDeliveryFailed(Id, failedAt, reason, attempt.AttemptNumber));
    }

    public void RecordBounce(DateTimeOffset bouncedAt, string reason)
    {
        var attempt = DeliveryAttempt.Bounced(
            Recipient.Channel,
            bouncedAt,
            _deliveryAttempts.Count + 1,
            reason);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Bounced;

        AddDomainEvent(new NotificationBounced(Id, bouncedAt, reason));
    }

    public void Cancel(DateTimeOffset cancelledAt, string reason)
    {
        Status = DeliveryStatus.Cancelled;

        AddDomainEvent(new NotificationCancelled(Id, cancelledAt, reason));
    }

    public void MarkRead(string userId, DateTimeOffset readAt)
    {
        if (ReadAt.HasValue)
        {
            return;
        }

        ReadAt = readAt;
        AddDomainEvent(new NotificationRead(Id, userId, readAt));
    }

    private static DateTimeOffset? ComputeScheduledTime(NotificationSchedule schedule, DateTimeOffset createdAt)
    {
        return schedule.Type switch
        {
            ScheduleType.Immediate => null,
            ScheduleType.Delayed when schedule.Delay.HasValue => createdAt.Add(schedule.Delay.Value),
            ScheduleType.Daily when schedule.DailyAt.HasValue =>
                ComputeNextDailyTime(createdAt, schedule.DailyAt.Value),
            ScheduleType.Weekly when schedule is { DaysOfWeek: not null, DailyAt: not null } =>
                ComputeNextWeeklyTime(createdAt, schedule.DaysOfWeek, schedule.DailyAt.Value),
            ScheduleType.Batched when schedule.BatchWindow.HasValue => createdAt.Add(schedule.BatchWindow.Value),
            _ => null
        };
    }

    private static DateTimeOffset ComputeNextDailyTime(DateTimeOffset from, TimeOnly dailyAt)
    {
        var todayAt = new DateTimeOffset(
            from.Year, from.Month, from.Day,
            dailyAt.Hour, dailyAt.Minute, dailyAt.Second,
            from.Offset);

        return todayAt > from ? todayAt : todayAt.AddDays(1);
    }

    private static DateTimeOffset ComputeNextWeeklyTime(DateTimeOffset from, DayOfWeek[] daysOfWeek, TimeOnly dailyAt)
    {
        for (var i = 0; i < 8; i++)
        {
            var candidate = from.AddDays(i);
            if (!daysOfWeek.Contains(candidate.DayOfWeek))
            {
                continue;
            }

            var candidateAt = new DateTimeOffset(
                candidate.Year, candidate.Month, candidate.Day,
                dailyAt.Hour, dailyAt.Minute, dailyAt.Second,
                from.Offset);

            if (candidateAt > from)
            {
                return candidateAt;
            }
        }

        return from.AddDays(7);
    }

    private void EnsureCanQueue()
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.Bounced or DeliveryStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot queue a completed notification.");
        }
    }
}