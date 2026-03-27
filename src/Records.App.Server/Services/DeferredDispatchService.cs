using System.Diagnostics;
using MediatR;
using Records.Core.Contracts;

namespace Records.App.Server.Services;

public sealed class DeferredDispatchService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeferredDispatchService> logger
) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryBackoff = TimeSpan.FromMinutes(5);
    private readonly Dictionary<long, FailedDispatchState> _failedDispatches = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DeferredDispatchProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedCount = 0;
            var outcome = "success";
            var startedAt = Stopwatch.GetTimestamp();
            using var activity = BackgroundServiceTelemetry.ActivitySource.StartActivity("DeferredDispatch.loop");

            try
            {
                processedCount = await ProcessDeferredEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outcome = "failure";
                logger.LogError(ex, "DeferredDispatchProcessor encountered an error");
            }
            finally
            {
                activity?.SetTag("service", "deferred-dispatch");
                activity?.SetTag("processed", processedCount);
                activity?.SetTag("outcome", outcome);
                var durationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                BackgroundServiceTelemetry.RecordRun(
                    "deferred-dispatch",
                    outcome,
                    processedCount,
                    durationMs);
            }

            try
            {
                var delay = processedCount > 0 ? PollingInterval : IdleInterval;
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("DeferredDispatchProcessor stopped");
    }

    private async Task<int> ProcessDeferredEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var serializer = scope.ServiceProvider.GetRequiredService<IDomainEventSerializer>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var pending = await eventStore.ReadUndispatchedAsync(BatchSize, cancellationToken);
        if (pending.Count == 0)
        {
            if (_failedDispatches.Count > 0)
            {
                _failedDispatches.Clear();
            }

            return 0;
        }

        PruneFailedDispatchState(pending);

        logger.LogDebug("Processing {Count} deferred event(s)", pending.Count);

        var successfulPositions = new List<long>();

        var now = DateTimeOffset.UtcNow;

        foreach (var storedEvent in pending)
        {
            if (_failedDispatches.TryGetValue(storedEvent.Position, out var failedState) &&
                now < failedState.NextAttemptAt)
            {
                continue;
            }

            try
            {
                var domainEvent = serializer.Deserialize(storedEvent.Payload, storedEvent.EventName);
                await mediator.Publish(domainEvent, cancellationToken);
                successfulPositions.Add(storedEvent.Position);
                _failedDispatches.Remove(storedEvent.Position);

                logger.LogTrace(
                    "Dispatched event {EventName} at position {Position}",
                    storedEvent.EventName,
                    storedEvent.Position);
            }
            catch (Exception ex)
            {
                var nextState = _failedDispatches.TryGetValue(storedEvent.Position, out failedState)
                    ? failedState.Next(ex.Message)
                    : FailedDispatchState.Initial(ex.Message);
                _failedDispatches[storedEvent.Position] = nextState;
                BackgroundServiceTelemetry.RecordRetryScheduled(
                    "deferred-dispatch",
                    "publish-failure");

                logger.LogError(
                    ex,
                    "Failed to dispatch event {EventName} at position {Position}. Attempt {Attempt}. Next retry in {RetryDelay}",
                    storedEvent.EventName,
                    storedEvent.Position,
                    nextState.Attempts,
                    nextState.NextAttemptAt - now);
            }
        }

        if (successfulPositions.Count > 0)
        {
            await eventStore.MarkDispatchedBatchAsync(successfulPositions, cancellationToken);

            logger.LogDebug(
                "Marked {Count} event(s) as dispatched",
                successfulPositions.Count);
        }

        return successfulPositions.Count;
    }

    private void PruneFailedDispatchState(IReadOnlyCollection<StoredEvent> pending)
    {
        if (_failedDispatches.Count == 0)
        {
            return;
        }

        var activePositions = pending.Select(e => e.Position).ToHashSet();
        var stale = _failedDispatches.Keys.Where(key => !activePositions.Contains(key)).ToList();
        foreach (var key in stale)
        {
            _failedDispatches.Remove(key);
        }
    }

    private sealed record FailedDispatchState(
        int Attempts,
        DateTimeOffset NextAttemptAt,
        string LastError
    )
    {
        public static FailedDispatchState Initial(string error)
        {
            return new FailedDispatchState(
                1,
                DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(2)),
                error);
        }

        public FailedDispatchState Next(string error)
        {
            var attempts = Attempts + 1;
            var seconds = Math.Min(Math.Pow(2, attempts), MaxRetryBackoff.TotalSeconds);
            return new FailedDispatchState(
                attempts,
                DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(seconds)),
                error);
        }
    }
}