using Microsoft.Extensions.Logging.Abstractions;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Tests.Commands;

public sealed class ProcessNotificationCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenProviderThrows_RecordsFailureAndPersists()
    {
        var notification = CreatePendingNotification(NotificationChannel.Email);
        var unitOfWork = new FakeNotificationsUnitOfWork(notification);
        var providers = new INotificationProvider[]
        {
            new ThrowingProvider(NotificationChannel.Email, "smtp unavailable")
        };
        var handler = new ProcessNotificationCommandHandler(
            unitOfWork,
            providers,
            NullLogger<ProcessNotificationCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessNotificationCommand(notification.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(unitOfWork.SaveChangesCallCount, Is.EqualTo(1));
            Assert.That(unitOfWork.Repository.UpdateCallCount, Is.EqualTo(1));
            Assert.That(notification.Status, Is.EqualTo(DeliveryStatus.Failed));
            Assert.That(notification.DeliveryAttempts.Count, Is.EqualTo(1));
            Assert.That(
                notification.DeliveryAttempts[0].FailureReason,
                Does.Contain("Provider exception: smtp unavailable"));
        });
    }

    [Test]
    public async Task Handle_WhenNoProviderConfigured_RecordsFailureAndPersists()
    {
        var notification = CreatePendingNotification(NotificationChannel.Push);
        var unitOfWork = new FakeNotificationsUnitOfWork(notification);
        var providers = new INotificationProvider[]
        {
            new FakeProvider(NotificationChannel.Email)
        };
        var handler = new ProcessNotificationCommandHandler(
            unitOfWork,
            providers,
            NullLogger<ProcessNotificationCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessNotificationCommand(notification.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(unitOfWork.SaveChangesCallCount, Is.EqualTo(1));
            Assert.That(unitOfWork.Repository.UpdateCallCount, Is.EqualTo(1));
            Assert.That(notification.Status, Is.EqualTo(DeliveryStatus.Failed));
            Assert.That(notification.DeliveryAttempts.Count, Is.EqualTo(1));
            Assert.That(
                notification.DeliveryAttempts[0].FailureReason,
                Does.Contain("No provider configured for channel Push"));
        });
    }

    private static Notification CreatePendingNotification(NotificationChannel channel)
    {
        var trigger = NotificationTrigger.RecordCreated(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            123);
        var recipient = channel switch
        {
            NotificationChannel.Email => NotificationRecipient.Email("ops@records.local", "Ops", "ops-user"),
            NotificationChannel.Push => NotificationRecipient.Push("device-token", "ops-user"),
            NotificationChannel.Sms => NotificationRecipient.Sms("+15551234567", "Ops", "ops-user"),
            NotificationChannel.Webhook => NotificationRecipient.Webhook("https://hooks.records.local"),
            _ => NotificationRecipient.InApp("ops-user")
        };

        var content = new NotificationContent
        {
            Subject = "Record Updated",
            Body = "Record was updated."
        };

        return Notification.Create(
            trigger,
            recipient,
            content,
            NotificationSchedule.Immediate(),
            NotificationPriority.Normal,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeNotificationsUnitOfWork(Notification notification) : INotificationsUnitOfWork
    {
        public FakeNotificationRepository Repository { get; } = new(notification);

        public int SaveChangesCallCount { get; private set; }

        public INotificationRepository Notifications => Repository;
        public INotificationRuleRepository NotificationRules { get; } = new FakeNotificationRuleRepository();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
            // no-op for tests
        }
    }

    private sealed class FakeNotificationRepository(Notification notification) : INotificationRepository
    {
        public int UpdateCallCount { get; private set; }

        public Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id == notification.Id ? notification : null);
        }

        public Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Notification>> GetByRecordsetIdAsync(
            UlidId recordsetId,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
            int maxAttempts,
            TimeSpan retryAfter,
            int limit,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task MarkAllAsReadAsync(
            string userId,
            DateTimeOffset readAt,
            DateTimeOffset? before = null,
            UlidId? recordsetId = null,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Notification notificationEntity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(Notification notificationEntity, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationRuleRepository : INotificationRuleRepository
    {
        public Task<NotificationRule?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<NotificationRule>> ListByRecordsetAsync(
            UlidId recordsetId,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<NotificationRule>> ListActiveByRecordsetAsync(
            UlidId recordsetId,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(NotificationRule rule, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(NotificationRule rule, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeProvider(NotificationChannel channel) : INotificationProvider
    {
        public NotificationChannel Channel => channel;

        public bool CanHandle(NotificationChannel targetChannel)
        {
            return targetChannel == channel;
        }

        public Task<NotificationSendResult> SendAsync(
            NotificationRecipient recipient,
            NotificationContent content,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(NotificationSendResult.Succeeded("provider-msg-1"));
        }
    }

    private sealed class ThrowingProvider(NotificationChannel channel, string errorMessage) : INotificationProvider
    {
        public NotificationChannel Channel => channel;

        public bool CanHandle(NotificationChannel targetChannel)
        {
            return targetChannel == channel;
        }

        public Task<NotificationSendResult> SendAsync(
            NotificationRecipient recipient,
            NotificationContent content,
            CancellationToken cancellationToken = default
        )
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
