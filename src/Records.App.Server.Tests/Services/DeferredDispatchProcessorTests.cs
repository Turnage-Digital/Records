using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Records.App.Server.Services;
using Records.Core.Contracts;

namespace Records.App.Server.Tests.Services;

public sealed class DeferredDispatchProcessorTests
{
    private Mock<IEventStore> eventStore = null!;
    private Mock<ILogger<DeferredDispatchProcessor>> logger = null!;
    private Mock<IMediator> mediator = null!;
    private IServiceScopeFactory scopeFactory = null!;
    private Mock<IDomainEventSerializer> serializer = null!;

    [SetUp]
    public void SetUp()
    {
        eventStore = new Mock<IEventStore>();
        serializer = new Mock<IDomainEventSerializer>();
        mediator = new Mock<IMediator>();
        logger = new Mock<ILogger<DeferredDispatchProcessor>>();

        var services = new ServiceCollection();
        services.AddSingleton(eventStore.Object);
        services.AddSingleton(serializer.Object);
        services.AddSingleton(mediator.Object);

        var serviceProvider = services.BuildServiceProvider();
        scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Test]
    public async Task ExecuteAsync_DispatchesPendingEvents_AndMarksDispatchedBatch()
    {
        var storedEvent = CreateStoredEvent(1, "Recordset:abc", "RecordCreated", "{\"id\":1}");
        var domainEvent = new TestDomainEvent();
        var markedPositions = new List<long>();
        var readCount = 0;

        eventStore
            .Setup(s => s.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                readCount++;
                return readCount == 1
                    ? (IReadOnlyList<StoredEvent>)[storedEvent]
                    : [];
            });

        serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        eventStore
            .Setup(s => s.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        var processor = new DeferredDispatchProcessor(scopeFactory, logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

        await processor.StartAsync(cts.Token);
        await Task.Delay(150, CancellationToken.None);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        Assert.That(markedPositions, Is.EquivalentTo(new[] { 1L }));
        mediator.Verify(
            m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenPublishFails_DoesNotMarkDispatched_AndBacksOffRetries()
    {
        var storedEvent = CreateStoredEvent(1, "Recordset:abc", "RecordCreated", "{\"id\":1}");
        var domainEvent = new TestDomainEvent();

        eventStore
            .Setup(s => s.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent]);

        serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var processor = new DeferredDispatchProcessor(scopeFactory, logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(450));

        await processor.StartAsync(cts.Token);
        await Task.Delay(220, CancellationToken.None);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        eventStore.Verify(
            s => s.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mediator.Verify(
            m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static StoredEvent CreateStoredEvent(
        long position,
        string streamId,
        string eventName,
        string payload
    )
    {
        return new StoredEvent(
            position,
            streamId,
            streamId.Split(':')[0],
            1,
            Guid.NewGuid().ToString(),
            eventName,
            payload,
            DateTime.UtcNow,
            null,
            null,
            null);
    }

    private sealed class TestDomainEvent : INotification;
}