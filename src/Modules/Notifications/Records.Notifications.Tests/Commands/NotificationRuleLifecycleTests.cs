using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Events;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Tests.Commands;

public sealed class NotificationRuleLifecycleTests
{
    [Test]
    public void Create_ShouldRaiseCreatedEvent_WhenRuleIsCreated()
    {
        var tenantId = UlidId.NewUlid();
        var recordsetId = UlidId.NewUlid();
        var trigger = NotificationTrigger.RecordCreated(tenantId, recordsetId, 42);
        var schedule = NotificationSchedule.Immediate();
        var channels = new[]
        {
            new NotificationChannelConfig
            {
                Type = NotificationChannel.InApp
            }
        };

        var rule = NotificationRule.Create(
            tenantId,
            recordsetId,
            "ops-user",
            trigger,
            channels,
            schedule,
            "seed.template",
            true);

        Assert.That(rule.DomainEvents.OfType<NotificationRuleCreated>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Update_ShouldRaiseUpdatedEvent_WhenRuleChanges()
    {
        var tenantId = UlidId.NewUlid();
        var recordsetId = UlidId.NewUlid();
        var rule = NotificationRule.Rehydrate(
            UlidId.NewUlid(),
            tenantId,
            recordsetId,
            "ops-user",
            NotificationTrigger.RecordCreated(tenantId, recordsetId, 42),
            [
                new NotificationChannelConfig
                {
                    Type = NotificationChannel.InApp
                }
            ],
            NotificationSchedule.Immediate(),
            null,
            true,
            false);

        rule.Update(
            NotificationTrigger.RecordUpdated(tenantId, recordsetId, 42),
            [
                new NotificationChannelConfig
                {
                    Type = NotificationChannel.Email,
                    Address = "ops@records.local"
                }
            ],
            NotificationSchedule.Delayed(TimeSpan.FromMinutes(10)),
            "notifications.updated",
            false);

        Assert.That(rule.DomainEvents.OfType<NotificationRuleUpdated>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Delete_ShouldRaiseDeletedEvent_WhenRuleIsDeleted()
    {
        var tenantId = UlidId.NewUlid();
        var recordsetId = UlidId.NewUlid();
        var rule = NotificationRule.Rehydrate(
            UlidId.NewUlid(),
            tenantId,
            recordsetId,
            "ops-user",
            NotificationTrigger.RecordCreated(tenantId, recordsetId, 42),
            [
                new NotificationChannelConfig
                {
                    Type = NotificationChannel.InApp
                }
            ],
            NotificationSchedule.Immediate(),
            null,
            true,
            false);

        rule.Delete();

        Assert.Multiple(() =>
        {
            Assert.That(rule.IsDeleted, Is.True);
            Assert.That(rule.DomainEvents.OfType<NotificationRuleDeleted>().Count(), Is.EqualTo(1));
        });
    }
}
