using System.Collections;
using System.Globalization;
using MediatR;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Services;
using Records.Notifications.Domain.ValueObjects;
using Records.Recordsets.Contracts.IntegrationEvents;

namespace Records.Notifications.Application.EventHandlers;

public sealed class RecordsetNotificationHandler(
    INotificationsUnitOfWork unitOfWork,
    INotificationTriggerEvaluator triggerEvaluator
) : INotificationHandler<RecordsetUpdatedIntegrationEvent>,
    INotificationHandler<RecordCreatedIntegrationEvent>,
    INotificationHandler<RecordUpdatedIntegrationEvent>,
    INotificationHandler<RecordDeletedIntegrationEvent>
{
    public async Task Handle(RecordsetUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        var matchingRules = rules
            .Where(r => r.Trigger.Type == NotificationTriggerType.ListUpdated)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return;
        }

        var context = new Dictionary<string, object>
        {
            ["RecordsetId"] = notification.RecordsetId.ToString(),
            ["UpdatedBy"] = notification.UpdatedBy.ToString(),
            ["UpdatedAt"] = notification.UpdatedAt
        };

        var createdCount = 0;

        foreach (var rule in matchingRules)
        {
            var trigger = NotificationTrigger.ListUpdated(rule.TenantId, notification.RecordsetId);

            if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
            {
                continue;
            }

            var content = BuildContent(
                rule,
                "Recordset Updated",
                $"Recordset {notification.RecordsetId} was updated by {notification.UpdatedBy}.",
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

    public async Task Handle(RecordCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        var matchingRules = rules
            .Where(r => r.Trigger.Type == NotificationTriggerType.ItemCreated)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return;
        }

        var context = new Dictionary<string, object>
        {
            ["RecordsetId"] = notification.RecordsetId.ToString(),
            ["RecordId"] = notification.RecordId ?? 0,
            ["CreatedBy"] = notification.CreatedBy.ToString(),
            ["CreatedAt"] = notification.CreatedAt
        };

        var createdCount = 0;

        foreach (var rule in matchingRules)
        {
            var trigger = new NotificationTrigger
            {
                Type = NotificationTriggerType.ItemCreated,
                TenantId = rule.TenantId,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId
            };

            if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
            {
                continue;
            }

            var content = BuildContent(
                rule,
                "Record Created",
                $"A record was created by {notification.CreatedBy}.",
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

    public async Task Handle(RecordUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        if (rules.Count == 0)
        {
            return;
        }

        var previousBag = ToDictionary(notification.PreviousBag);
        var newBag = ToDictionary(notification.NewBag);
        var context = BuildContext(notification, previousBag, newBag);
        var actualTriggers = BuildActualTriggers(notification, previousBag, newBag);

        var createdCount = 0;

        foreach (var rule in rules)
        {
            foreach (var actualTrigger in actualTriggers)
            {
                if (rule.Trigger.Type != actualTrigger.Type)
                {
                    continue;
                }

                var trigger = actualTrigger with { TenantId = rule.TenantId };

                if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
                {
                    continue;
                }

                var content = BuildContent(
                    rule,
                    "Record Updated",
                    $"Record {notification.RecordId} was updated by {notification.UpdatedBy}.",
                    context);

                createdCount += await CreateNotificationsAsync(
                    rule,
                    trigger,
                    content,
                    notification.OccurredOn,
                    notification.EventId.ToString(),
                    cancellationToken);

                break;
            }
        }

        if (createdCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(RecordDeletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var rules = await unitOfWork.NotificationRules.ListActiveByRecordsetAsync(
            notification.RecordsetId,
            cancellationToken);

        var matchingRules = rules
            .Where(r => r.Trigger.Type == NotificationTriggerType.ItemDeleted)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return;
        }

        var context = new Dictionary<string, object>
        {
            ["RecordsetId"] = notification.RecordsetId.ToString(),
            ["RecordId"] = notification.RecordId ?? 0,
            ["DeletedBy"] = notification.DeletedBy.ToString(),
            ["DeletedAt"] = notification.DeletedAt
        };

        var createdCount = 0;

        foreach (var rule in matchingRules)
        {
            var trigger = new NotificationTrigger
            {
                Type = NotificationTriggerType.ItemDeleted,
                TenantId = rule.TenantId,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId
            };

            if (!await triggerEvaluator.ShouldTriggerAsync(rule, trigger, context, cancellationToken))
            {
                continue;
            }

            var content = BuildContent(
                rule,
                "Record Deleted",
                $"Record {notification.RecordId} was deleted by {notification.DeletedBy}.",
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

    private static Dictionary<string, object> BuildContext(
        RecordUpdatedIntegrationEvent notification,
        Dictionary<string, object?> previousBag,
        Dictionary<string, object?> newBag
    )
    {
        var context = new Dictionary<string, object>
        {
            ["RecordsetId"] = notification.RecordsetId.ToString(),
            ["RecordId"] = notification.RecordId ?? 0,
            ["UpdatedBy"] = notification.UpdatedBy.ToString(),
            ["UpdatedAt"] = notification.UpdatedAt,
            ["NewBag"] = newBag
        };

        if (previousBag.Count > 0)
        {
            context["PreviousBag"] = previousBag;
        }

        foreach (var kvp in newBag)
        {
            if (kvp.Value is not null)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        foreach (var kvp in previousBag)
        {
            if (kvp.Value is not null)
            {
                context[$"previous.{kvp.Key}"] = kvp.Value;
            }
        }

        return context;
    }

    private static IReadOnlyCollection<NotificationTrigger> BuildActualTriggers(
        RecordUpdatedIntegrationEvent notification,
        Dictionary<string, object?> previousBag,
        Dictionary<string, object?> newBag
    )
    {
        var triggers = new List<NotificationTrigger>
        {
            new()
            {
                Type = NotificationTriggerType.ItemUpdated,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId
            }
        };

        var previousStatus = GetString(previousBag, "status");
        var newStatus = GetString(newBag, "status");

        if (!string.Equals(previousStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            triggers.Add(new NotificationTrigger
            {
                Type = NotificationTriggerType.StatusChanged,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId,
                FromValue = previousStatus,
                ToValue = newStatus
            });
        }

        foreach (var change in GetColumnChanges(previousBag, newBag))
        {
            triggers.Add(new NotificationTrigger
            {
                Type = NotificationTriggerType.ColumnValueChanged,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId,
                ColumnName = change.Column,
                FromValue = change.Previous,
                ToValue = change.Current
            });
        }

        if (newBag.Count > 0)
        {
            triggers.Add(new NotificationTrigger
            {
                Type = NotificationTriggerType.CustomCondition,
                RecordsetId = notification.RecordsetId,
                RecordId = notification.RecordId
            });
        }

        return triggers;
    }

    private static IEnumerable<(string Column, string? Previous, string? Current)> GetColumnChanges(
        Dictionary<string, object?> previousBag,
        Dictionary<string, object?> newBag
    )
    {
        var keys = new HashSet<string>(previousBag.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(newBag.Keys);

        foreach (var key in keys)
        {
            if (string.Equals(key, "status", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var previousValue = GetString(previousBag, key);
            var currentValue = GetString(newBag, key);

            if (string.Equals(previousValue, currentValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return (key, previousValue, currentValue);
        }
    }

    private static string? GetString(Dictionary<string, object?> bag, string key)
    {
        if (!bag.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        if (value is string s)
        {
            return s;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, object?> ToDictionary(object? bag)
    {
        if (bag is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (bag is IDictionary<string, object?> typed)
        {
            return new Dictionary<string, object?>(typed, StringComparer.OrdinalIgnoreCase);
        }

        if (bag is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key)
                {
                    result[key] = entry.Value;
                }
            }

            return result;
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
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
            var notification = Notification.Create(
                trigger,
                recipient,
                content,
                rule.Schedule,
                NotificationPriority.Normal,
                createdAt,
                correlationId,
                rule.Id);

            await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        }

        return recipients.Count;
    }
}
