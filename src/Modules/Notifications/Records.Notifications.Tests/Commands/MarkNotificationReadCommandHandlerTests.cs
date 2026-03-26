using Records.Core.Domain.ValueObjects;
using Records.Core.Application;
using Records.Notifications.Application.Commands;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Tests.Commands;

public sealed class MarkNotificationReadCommandHandlerTests
{
    [Test]
    public async Task Handle_ShouldMarkNotificationRead_WhenCurrentUserOwnsNotification()
    {
        var notification = CreateNotification("ops-user");
        var unitOfWork = new FakeNotificationsUnitOfWork(notification);
        var handler = new MarkNotificationReadCommandHandler(unitOfWork);
        var command = new MarkNotificationReadCommand(notification.Id, DateTimeOffset.UtcNow)
        {
            UserId = "ops-user"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(notification.ReadAt, Is.Not.Null);
            Assert.That(unitOfWork.SaveChangesCallCount, Is.EqualTo(1));
            Assert.That(unitOfWork.Repository.UpdateCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Handle_ShouldFailForbidden_WhenCurrentUserDoesNotOwnNotification()
    {
        var notification = CreateNotification("ops-user");
        var unitOfWork = new FakeNotificationsUnitOfWork(notification);
        var handler = new MarkNotificationReadCommandHandler(unitOfWork);
        var command = new MarkNotificationReadCommand(notification.Id, DateTimeOffset.UtcNow)
        {
            UserId = "other-user"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(ResultErrors.Forbidden));
            Assert.That(notification.ReadAt, Is.Null);
            Assert.That(unitOfWork.SaveChangesCallCount, Is.EqualTo(0));
            Assert.That(unitOfWork.Repository.UpdateCallCount, Is.EqualTo(0));
        });
    }

    private static Notification CreateNotification(string recipientUserId)
    {
        var trigger = NotificationTrigger.RecordCreated(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            123);

        return Notification.Create(
            trigger,
            NotificationRecipient.InApp(recipientUserId),
            new NotificationContent
            {
                Subject = "Record updated",
                Body = "Record was updated."
            },
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
        }
    }

    private sealed class FakeNotificationRepository(Notification notification) : INotificationRepository
    {
        public int UpdateCallCount { get; private set; }

        public Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id == notification.Id ? notification : null);
        }

        public Task<IReadOnlyList<Notification>> GetPendingAsync(int limit,
            CancellationToken cancellationToken = default)
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
}