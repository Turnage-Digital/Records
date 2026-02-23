using MediatR;
using Records.Clocks.Contracts.IntegrationEvents;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Services;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Application.EventHandlers;

public sealed class ClockNotificationHandler(
    INotificationsUnitOfWork unitOfWork,
    INotificationTriggerEvaluator triggerEvaluator
) : INotificationHandler<ClockAtRiskIntegrationEvent>,
    INotificationHandler<ClockBreachedIntegrationEvent>
{
    public async Task Handle(ClockAtRiskIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        var matchingRules = rules
            .Where(r => r.Trigger.Type == NotificationTriggerType.ClockAtRisk)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return;
        }

        var context = BuildClockContext(notification.ClockName, notification.AtRiskAt, notification.BreachDueAt);
        var createdCount = 0;

        foreach (var rule in matchingRules)
        {
            var trigger = NotificationTrigger.ClockAtRisk(
                rule.TenantId,
                notification.RecordsetId,
                notification.RecordId,
                notification.ClockName);

            if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
            {
                continue;
            }

            var content = BuildContent(
                rule,
                "Clock At Risk",
                $"Clock \"{notification.ClockName}\" is at risk.",
                context);

            createdCount += await CreateNotificationsAsync(
                rule,
                trigger,
                content,
                notification.OccurredOn,
                notification.EventId.ToString(),
                cancellationToken);
        }

        if (createdCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(ClockBreachedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        var matchingRules = rules
            .Where(r => r.Trigger.Type == NotificationTriggerType.ClockBreached)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return;
        }

        var context = BuildClockContext(notification.ClockName, notification.BreachedAt, notification.BreachDueAt);
        var createdCount = 0;

        foreach (var rule in matchingRules)
        {
            var trigger = NotificationTrigger.ClockBreached(
                rule.TenantId,
                notification.RecordsetId,
                notification.RecordId,
                notification.ClockName);

            if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
            {
                continue;
            }

            var content = BuildContent(
                rule,
                "Clock Breached",
                $"Clock \"{notification.ClockName}\" has breached its deadline.",
                context);

            createdCount += await CreateNotificationsAsync(
                rule,
                trigger,
                content,
                notification.OccurredOn,
                notification.EventId.ToString(),
                cancellationToken);
        }

        if (createdCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static Dictionary<string, object> BuildClockContext(
        string clockName,
        DateTimeOffset occurredAt,
        DateTimeOffset breachDueAt
    )
    {
        return new Dictionary<string, object>
        {
            ["ClockName"] = clockName,
            ["OccurredAt"] = occurredAt,
            ["BreachDueAt"] = breachDueAt
        };
    }

    private static NotificationContent BuildContent(
        NotificationRule rule,
        string subject,
        string body,
        IReadOnlyDictionary<string, object> templateData
    )
    {
        return new NotificationContent
        {
            Subject = subject,
            Body = body,
            TemplateId = rule.TemplateId,
            TemplateData = templateData
        };
    }

    private static IReadOnlyList<NotificationRecipient> BuildRecipients(NotificationRule rule)
    {
        var recipients = new List<NotificationRecipient>();

        if (rule.Channels.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(rule.UserId))
            {
                recipients.Add(NotificationRecipient.InApp(rule.UserId));
            }

            return recipients;
        }

        foreach (var channel in rule.Channels)
        {
            switch (channel.Type)
            {
                case NotificationChannel.InApp:
                    if (!string.IsNullOrWhiteSpace(rule.UserId))
                    {
                        recipients.Add(NotificationRecipient.InApp(rule.UserId));
                    }

                    break;
                case NotificationChannel.Email:
                    if (!string.IsNullOrWhiteSpace(channel.Address))
                    {
                        recipients.Add(NotificationRecipient.Email(channel.Address, userId: rule.UserId));
                    }

                    break;
                case NotificationChannel.Sms:
                    if (!string.IsNullOrWhiteSpace(channel.Address))
                    {
                        recipients.Add(NotificationRecipient.Sms(channel.Address, userId: rule.UserId));
                    }

                    break;
                case NotificationChannel.Push:
                    if (!string.IsNullOrWhiteSpace(channel.Address))
                    {
                        recipients.Add(NotificationRecipient.Push(channel.Address, rule.UserId));
                    }

                    break;
                case NotificationChannel.Webhook:
                    if (!string.IsNullOrWhiteSpace(channel.Address))
                    {
                        recipients.Add(NotificationRecipient.Webhook(channel.Address, channel.Settings));
                    }

                    break;
            }
        }

        return recipients;
    }

    private async Task<int> CreateNotificationsAsync(
        NotificationRule rule,
        NotificationTrigger trigger,
        NotificationContent content,
        DateTimeOffset createdAt,
        string? correlationId,
        CancellationToken cancellationToken
    )
    {
        var recipients = BuildRecipients(rule);
        if (recipients.Count == 0)
        {
            return 0;
        }

        foreach (var recipient in recipients)
        {
            var notificationEntity = Notification.Create(
                trigger,
                recipient,
                content,
                rule.Schedule,
                NotificationPriority.Normal,
                createdAt,
                correlationId,
                rule.Id);

            await unitOfWork.Notifications.AddAsync(notificationEntity, cancellationToken);
        }

        return recipients.Count;
    }
}