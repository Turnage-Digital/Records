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
    private Mock<ILogger<NotificationProcessingService>> _logger = null!;
    private Mock<INotificationQueries> _notificationQueries = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<ISender> _sender = null!;

    [SetUp]
    public void SetUp()
    {
        _notificationQueries = new Mock<INotificationQueries>();
        _sender = new Mock<ISender>();
        _logger = new Mock<ILogger<NotificationProcessingService>>();

        var services = new ServiceCollection();
        services.AddSingleton(_notificationQueries.Object);
        services.AddSingleton(_sender.Object);

        var serviceProvider = services.BuildServiceProvider();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Test]
    public async Task ExecuteAsync_ProcessesPendingAndRetryNotifications_WithDeduplication()
    {
        var idA = Ulid.NewUlid().ToString();
        var idB = Ulid.NewUlid().ToString();

        var pendingCalls = 0;
        _notificationQueries
            .Setup(q => q.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                pendingCalls++;
                return pendingCalls == 1
                    ? (IReadOnlyList<NotificationPendingDto>)[CreatePending(idA)]
                    : [];
            });

        var retryCalls = 0;
        _notificationQueries
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
        _sender
            .Setup(s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result>, CancellationToken>((request, _) =>
            {
                if (request is ProcessNotificationCommand command)
                {
                    processedIds.Add(command.NotificationId.ToString());
                }
            })
            .ReturnsAsync(Result.Success());

        var service = new NotificationProcessingService(_scopeFactory, _logger.Object);
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
        _notificationQueries
            .Setup(q => q.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _notificationQueries
            .Setup(q => q.GetFailedForRetryAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new NotificationProcessingService(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await service.StartAsync(cts.Token);
        await Task.Delay(120, CancellationToken.None);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        _sender.Verify(
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