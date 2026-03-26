using Microsoft.EntityFrameworkCore;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Tests.Queries;

public sealed class NotificationQueriesTests
{
    [Test]
    public async Task GetFailedForRetryAsync_ReturnsOnlyEligibleNotifications()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTime.UtcNow;

        var eligibleId = Ulid.NewUlid().ToString();
        dbContext.Notifications.Add(new NotificationDb
        {
            Id = eligibleId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            RecipientAddress = "ops@records.local",
            ContentSubject = "Eligible",
            ContentBody = "Eligible",
            ContentTemplateDataJson = "{}",
            RecipientMetadataJson = "{}",
            ScheduleJson = "{}",
            Priority = 1,
            Status = (int)DeliveryStatus.Failed,
            DeliveryAttempts =
            [
                new DeliveryAttemptDb
                {
                    Channel = (int)NotificationChannel.Email,
                    Status = (int)DeliveryStatus.Failed,
                    AttemptedAt = now.AddMinutes(-10),
                    AttemptNumber = 1,
                    FailureReason = "retryable"
                }
            ]
        });
        dbContext.NotificationProjections.Add(new NotificationProjectionDb
        {
            Id = eligibleId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            Status = (int)DeliveryStatus.Failed,
            CreatedAt = now.AddMinutes(-20),
            AttemptCount = 1
        });

        var notDueYetId = Ulid.NewUlid().ToString();
        dbContext.Notifications.Add(new NotificationDb
        {
            Id = notDueYetId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            RecipientAddress = "ops@records.local",
            ContentSubject = "Not due yet",
            ContentBody = "Not due yet",
            ContentTemplateDataJson = "{}",
            RecipientMetadataJson = "{}",
            ScheduleJson = "{}",
            Priority = 1,
            Status = (int)DeliveryStatus.Failed,
            DeliveryAttempts =
            [
                new DeliveryAttemptDb
                {
                    Channel = (int)NotificationChannel.Email,
                    Status = (int)DeliveryStatus.Failed,
                    AttemptedAt = now.AddMinutes(-1),
                    AttemptNumber = 1,
                    FailureReason = "wait more",
                    NextRetryAfter = TimeSpan.FromHours(1)
                }
            ]
        });
        dbContext.NotificationProjections.Add(new NotificationProjectionDb
        {
            Id = notDueYetId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            Status = (int)DeliveryStatus.Failed,
            CreatedAt = now.AddMinutes(-19),
            AttemptCount = 1
        });

        var maxAttemptsId = Ulid.NewUlid().ToString();
        dbContext.Notifications.Add(new NotificationDb
        {
            Id = maxAttemptsId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            RecipientAddress = "ops@records.local",
            ContentSubject = "Max attempts reached",
            ContentBody = "Max attempts reached",
            ContentTemplateDataJson = "{}",
            RecipientMetadataJson = "{}",
            ScheduleJson = "{}",
            Priority = 1,
            Status = (int)DeliveryStatus.Failed,
            DeliveryAttempts =
            [
                new DeliveryAttemptDb
                {
                    Channel = (int)NotificationChannel.Email,
                    Status = (int)DeliveryStatus.Failed,
                    AttemptedAt = now.AddMinutes(-30),
                    AttemptNumber = 1,
                    FailureReason = "attempt 1"
                },
                new DeliveryAttemptDb
                {
                    Channel = (int)NotificationChannel.Email,
                    Status = (int)DeliveryStatus.Failed,
                    AttemptedAt = now.AddMinutes(-20),
                    AttemptNumber = 2,
                    FailureReason = "attempt 2"
                },
                new DeliveryAttemptDb
                {
                    Channel = (int)NotificationChannel.Email,
                    Status = (int)DeliveryStatus.Failed,
                    AttemptedAt = now.AddMinutes(-10),
                    AttemptNumber = 3,
                    FailureReason = "attempt 3"
                }
            ]
        });
        dbContext.NotificationProjections.Add(new NotificationProjectionDb
        {
            Id = maxAttemptsId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            Status = (int)DeliveryStatus.Failed,
            CreatedAt = now.AddMinutes(-18),
            AttemptCount = 3
        });

        var noAttemptsId = Ulid.NewUlid().ToString();
        dbContext.Notifications.Add(new NotificationDb
        {
            Id = noAttemptsId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            RecipientAddress = "ops@records.local",
            ContentSubject = "No attempts",
            ContentBody = "No attempts",
            ContentTemplateDataJson = "{}",
            RecipientMetadataJson = "{}",
            ScheduleJson = "{}",
            Priority = 1,
            Status = (int)DeliveryStatus.Failed
        });
        dbContext.NotificationProjections.Add(new NotificationProjectionDb
        {
            Id = noAttemptsId,
            TenantId = Ulid.NewUlid().ToString(),
            TriggerType = (int)NotificationTriggerType.RecordUpdated,
            Channel = (int)NotificationChannel.Email,
            Status = (int)DeliveryStatus.Failed,
            CreatedAt = now.AddMinutes(-17),
            AttemptCount = 0
        });

        await dbContext.SaveChangesAsync();

        var queries = new NotificationQueries(dbContext);
        var results = await queries.GetFailedForRetryAsync(
            3,
            TimeSpan.FromMinutes(5),
            10,
            CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Id, Is.EqualTo(eligibleId));
        Assert.That(results[0].AttemptCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetFailedForRetryAsync_ReturnsEmpty_WhenLimitIsZero()
    {
        await using var dbContext = CreateDbContext();
        var queries = new NotificationQueries(dbContext);

        var results = await queries.GetFailedForRetryAsync(
            5,
            TimeSpan.FromMinutes(1),
            0,
            CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    private static NotificationsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"notifications-tests-{Guid.NewGuid():N}")
            .Options;
        return new NotificationsDbContext(options);
    }
}
