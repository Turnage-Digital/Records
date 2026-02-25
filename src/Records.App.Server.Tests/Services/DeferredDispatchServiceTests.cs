using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Records.App.Server.Services;
using Records.Core.Contracts;

namespace Records.App.Server.Tests.Services;

public sealed class DeferredDispatchServiceTests
{
    private Mock<IEventStore> _eventStore = null!;
    private Mock<ILogger<DeferredDispatchService>> _logger = null!;
    private Mock<IMediator> _mediator = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<IDomainEventSerializer> _serializer = null!;

    [SetUp]
    public void SetUp()
    {
        _eventStore = new Mock<IEventStore>();
        _serializer = new Mock<IDomainEventSerializer>();
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<DeferredDispatchService>>();

        var services = new ServiceCollection();
        services.AddSingleton(_eventStore.Object);
        services.AddSingleton(_serializer.Object);
        services.AddSingleton(_mediator.Object);

        var serviceProvider = services.BuildServiceProvider();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    [Test]
    public async Task ExecuteAsync_DispatchesPendingEvents_AndMarksDispatchedBatch()
    {
        var storedEvent = CreateStoredEvent(1, "Recordset:abc", "RecordCreated", "{\"id\":1}");
        var domainEvent = new TestDomainEvent();
        var markedPositions = new List<long>();
        var readCount = 0;

        _eventStore
            .Setup(s => s.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                readCount++;
                return readCount == 1
                    ? (IReadOnlyList<StoredEvent>)[storedEvent]
                    : [];
            });

        _serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        _mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStore
            .Setup(s => s.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<long>, CancellationToken>((positions, _) => markedPositions.AddRange(positions))
            .Returns(Task.CompletedTask);

        var processor = new DeferredDispatchService(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

        await processor.StartAsync(cts.Token);
        await Task.Delay(150, CancellationToken.None);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        Assert.That(markedPositions, Is.EquivalentTo(new[] { 1L }));
        _mediator.Verify(
            m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenPublishFails_DoesNotMarkDispatched_AndBacksOffRetries()
    {
        var storedEvent = CreateStoredEvent(1, "Recordset:abc", "RecordCreated", "{\"id\":1}");
        var domainEvent = new TestDomainEvent();

        _eventStore
            .Setup(s => s.ReadUndispatchedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([storedEvent]);

        _serializer
            .Setup(s => s.Deserialize(storedEvent.Payload, storedEvent.EventName))
            .Returns(domainEvent);

        _mediator
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var processor = new DeferredDispatchService(_scopeFactory, _logger.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(450));

        await processor.StartAsync(cts.Token);
        await Task.Delay(220, CancellationToken.None);
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        _eventStore.Verify(
            s => s.MarkDispatchedBatchAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(
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