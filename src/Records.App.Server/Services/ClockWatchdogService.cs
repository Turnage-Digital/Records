using MediatR;
using Records.Clocks.Application.Commands.Clocks.MarkAtRisk;
using Records.Clocks.Application.Commands.Clocks.MarkBreached;
using Records.Clocks.Contracts.Queries;
using Records.Core.Domain.ValueObjects;

namespace Records.App.Server.Services;

public sealed class ClockWatchdogService(
    IServiceScopeFactory scopeFactory,
    ILogger<ClockWatchdogService> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ClockWatchdogService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckClocksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ClockWatchdogService encountered an error");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("ClockWatchdogService stopped");
    }

    private async Task CheckClocksAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IClockQueries>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var now = DateTimeOffset.UtcNow;

        var atRiskClocks = await queries.GetRunningClocksPastThresholdAsync(now, cancellationToken);
        if (atRiskClocks.Count > 0)
        {
            logger.LogInformation("Found {Count} clocks past at-risk threshold", atRiskClocks.Count);
        }

        foreach (var clock in atRiskClocks)
        {
            if (!UlidId.TryParse(clock.ClockId, out var clockId))
            {
                logger.LogWarning("Invalid clock id in watchdog queue: {ClockId}", clock.ClockId);
                continue;
            }

            try
            {
                var command = new MarkClockAtRiskCommand(clockId, now);
                await sender.Send(command, cancellationToken);
                logger.LogInformation("Marked clock {ClockId} as at-risk", clockId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark clock {ClockId} as at-risk", clockId);
            }
        }

        var breachedClocks = await queries.GetRunningClocksPastDeadlineAsync(now, cancellationToken);
        if (breachedClocks.Count > 0)
        {
            logger.LogWarning("Found {Count} clocks past deadline", breachedClocks.Count);
        }

        foreach (var clock in breachedClocks)
        {
            if (!UlidId.TryParse(clock.ClockId, out var clockId))
            {
                logger.LogWarning("Invalid clock id in watchdog queue: {ClockId}", clock.ClockId);
                continue;
            }

            try
            {
                var command = new MarkClockBreachedCommand(clockId, now);
                await sender.Send(command, cancellationToken);
                logger.LogWarning("Marked clock {ClockId} as breached", clockId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark clock {ClockId} as breached", clockId);
            }
        }
    }
}