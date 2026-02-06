using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Records.App.Server.Services;
using Records.Clocks.Application.Commands.Clocks.MarkAtRisk;
using Records.Clocks.Application.Commands.Clocks.MarkBreached;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Tests.Services;

public sealed class ClockWatchdogServiceTests
{
    private Mock<ILogger<ClockWatchdogService>> _loggerMock = null!;
    private Mock<IClockQueries> _queriesMock = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<ISender> _senderMock = null!;

    [SetUp]
    public void Setup()
    {
        _queriesMock = new Mock<IClockQueries>();
        _senderMock = new Mock<ISender>();
        _loggerMock = new Mock<ILogger<ClockWatchdogService>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IClockQueries)))
            .Returns(_queriesMock.Object);
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ISender)))
            .Returns(_senderMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);

        _scopeFactory = scopeFactoryMock.Object;
    }

    [Test]
    public async Task ExecuteAsync_MarksAtRiskClocks_WhenPastThreshold()
    {
        var clock = new ClockWatchdogDto(
            UlidId.NewUlid().ToString(),
            UlidId.NewUlid().ToString(),
            12,
            UlidId.NewUlid().ToString(),
            UlidId.NewUlid().ToString(),
            ClockState.Running,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow.AddHours(-1)
        );

        _queriesMock
            .Setup(q => q.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClockWatchdogDto> { clock });
        _queriesMock
            .Setup(q => q.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClockWatchdogDto>());

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Test]
    public async Task ExecuteAsync_MarksBreachedClocks_WhenPastDeadline()
    {
        var clock = new ClockWatchdogDto(
            UlidId.NewUlid().ToString(),
            UlidId.NewUlid().ToString(),
            12,
            UlidId.NewUlid().ToString(),
            UlidId.NewUlid().ToString(),
            ClockState.AtRisk,
            DateTimeOffset.UtcNow.AddHours(-4),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(-2)
        );

        _queriesMock
            .Setup(q => q.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClockWatchdogDto>());
        _queriesMock
            .Setup(q => q.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClockWatchdogDto> { clock });

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }
}