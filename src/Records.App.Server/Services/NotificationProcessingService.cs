using System.Diagnostics;
using MediatR;
using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands;
using Records.Notifications.Contracts;

namespace Records.App.Server.Services;

public sealed class NotificationProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationProcessingService> logger
) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetryAttempts = 8;
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ActivePollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan IdlePollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationProcessingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedCount = 0;
            var outcome = "success";
            var startedAt = Stopwatch.GetTimestamp();
            using var activity = BackgroundServiceTelemetry.ActivitySource.StartActivity(
                "NotificationProcessing.loop");

            try
            {
                processedCount = await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outcome = "failure";
                logger.LogError(ex, "NotificationProcessingService encountered an error");
            }
            finally
            {
                activity?.SetTag("service", "notification-processing");
                activity?.SetTag("processed", processedCount);
                activity?.SetTag("outcome", outcome);
                var durationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                BackgroundServiceTelemetry.RecordRun(
                    "notification-processing",
                    outcome,
                    processedCount,
                    durationMs);
            }

            try
            {
                var delay = processedCount > 0 ? ActivePollingInterval : IdlePollingInterval;
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("NotificationProcessingService stopped");
    }

    private async Task<int> ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var notificationQueries = scope.ServiceProvider.GetRequiredService<INotificationQueries>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var pending = await notificationQueries.GetPendingAsync(BatchSize, cancellationToken);
        var workItems = pending.ToList();

        if (workItems.Count < BatchSize)
        {
            var remaining = BatchSize - workItems.Count;
            var retryCandidates = await notificationQueries.GetFailedForRetryAsync(
                MaxRetryAttempts,
                DefaultRetryAfter,
                remaining,
                cancellationToken);

            var seen = new HashSet<string>(workItems.Select(x => x.Id), StringComparer.Ordinal);
            foreach (var candidate in retryCandidates)
            {
                if (seen.Add(candidate.Id))
                {
                    workItems.Add(candidate);
                }
            }
        }

        if (workItems.Count is 0)
        {
            logger.LogTrace("No pending notifications to process");
            return 0;
        }

        logger.LogInformation(
            "Processing {Count} notification(s) (pending + retries)",
            workItems.Count);

        var processed = 0;
        var failed = 0;

        foreach (var notification in workItems)
        {
            try
            {
                var command = new ProcessNotificationCommand(UlidId.Parse(notification.Id))
                {
                    UserId = SystemActors.NotificationProcessor
                };
                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    processed += 1;
                    logger.LogDebug(
                        "Notification {NotificationId} processed successfully",
                        notification.Id);
                }
                else
                {
                    failed += 1;
                    BackgroundServiceTelemetry.RecordRetryScheduled(
                        "notification-processing",
                        "command-failed");
                    logger.LogWarning(
                        "Notification {NotificationId} processing failed: {Error}",
                        notification.Id,
                        result.Error);
                }
            }
            catch (Exception ex)
            {
                failed += 1;
                BackgroundServiceTelemetry.RecordRetryScheduled(
                    "notification-processing",
                    "exception");
                logger.LogError(
                    ex,
                    "Exception processing notification {NotificationId}",
                    notification.Id);
            }
        }

        if (failed > 0)
        {
            logger.LogWarning(
                "NotificationProcessingService completed batch with {Processed} success and {Failed} failed notification(s)",
                processed,
                failed);
        }

        return processed;
    }
}
