using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Records.App.Server.Services;
using Records.Core.Application;
using Records.Notifications.Application.Commands;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Contracts.Queries;
using Records.Notifications.Domain;

namespace Records.App.Server.Tests.Services;

public sealed class NotificationProcessingServiceTests
{
    private Mock<ILogger<NotificationProcessingService>> logger = null!;
    private Mock<INotificationQueries> notificationQueries = null!;
    private IServiceScopeFactory scopeFactory = null!;
    private Mock<ISender> sender = null!;

    [SetUp]
    public void SetUp()
    {
        notificationQueries = new Mock<INotificationQueries>();
        sender = new Mock<ISender>();
        logger = new Mock<ILogger<NotificationProcessingService>>();

        var services = new ServiceCollection();
        services.AddSingleton(notificationQueries.Object);
        services.AddSingleton(sender.Object);

        var serviceProvider = services.BuildServiceProvider();
        scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Test]
    public async Task ExecuteAsync_ProcessesPendingAndRetryNotifications_WithDeduplication()
    {
        var idA = Ulid.NewUlid().ToString();
        var idB = Ulid.NewUlid().ToString();

        var pendingCalls = 0;
        notificationQueries
            .Setup(q => q.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                pendingCalls++;
                return pendingCalls == 1
                    ? (IReadOnlyList<NotificationPendingDto>)[CreatePending(idA)]
                    : [];
            });

        var retryCalls = 0;
        notificationQueries
            .Setup(q => q.GetFailedForRetryAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                retryCalls++;
                return retryCalls == 1
                    ? (IReadOnlyList<NotificationPendingDto>)[CreatePending(idA), CreatePending(idB)]
                    : [];
            });

        var processedIds = new HashSet<string>(StringComparer.Ordinal);
        sender
            .Setup(s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result>, CancellationToken>((request, _) =>
            {
                if (request is ProcessNotificationCommand command)
                {
                    processedIds.Add(command.NotificationId.ToString());
                }
            })
            .ReturnsAsync(Result.Success());

        var service = new NotificationProcessingService(scopeFactory, logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(450));

        await service.StartAsync(cts.Token);
        await Task.Delay(220, CancellationToken.None);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.That(processedIds, Is.EquivalentTo(new[] { idA, idB }));
    }

    [Test]
    public async Task ExecuteAsync_WhenNoWork_DoesNotSendCommands()
    {
        notificationQueries
            .Setup(q => q.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        notificationQueries
            .Setup(q => q.GetFailedForRetryAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new NotificationProcessingService(scopeFactory, logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await service.StartAsync(cts.Token);
        await Task.Delay(120, CancellationToken.None);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        sender.Verify(
            s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static NotificationPendingDto CreatePending(string id)
    {
        return new NotificationPendingDto(
            id,
            Ulid.NewUlid().ToString(),
            Ulid.NewUlid().ToString(),
            321,
            NotificationTriggerType.RecordUpdated,
            NotificationChannel.Email,
            "ops@records.local",
            "ops-user",
            DeliveryStatus.Pending,
            1,
            null);
    }
}