using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.App.Server.Services;

public class RecordsetMigrationDispatcherService(
    ILogger<RecordsetMigrationDispatcherService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private const int MaxAttempts = 8;
    private static readonly TimeSpan RunningRecoveryThreshold = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RecordsetMigrationDispatcher: service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            RecordsetMigrationJobDb? job = null;
            var processed = 0;
            var outcome = "success";
            var startedAt = Stopwatch.GetTimestamp();
            using var activity = BackgroundServiceTelemetry.ActivitySource.StartActivity(
                "RecordsetMigrationDispatcher.loop");
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RecordsetsDbContext>();
                var runner = scope.ServiceProvider.GetRequiredService<RecordsetMigrationJobRunner>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IRecordsetsUnitOfWork>();
                await RecoverStaleRunningJobsAsync(dbContext, logger, stoppingToken);
                job = await GetNextJobAsync(dbContext, stoppingToken);
                if (job is null)
                {
                    logger.LogDebug("RecordsetMigrationDispatcher: no pending jobs");
                }
                else
                {
                    await ProcessJobAsync(job, dbContext, runner, logger, stoppingToken);
                    processed = 1;
                }

                await CleanupExpiredBackupsAsync(dbContext, unitOfWork, logger, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                outcome = "failure";
                logger.LogError(ex, "RecordsetMigrationDispatcher: loop error");
            }
            finally
            {
                activity?.SetTag("service", "recordset-migration-dispatcher");
                activity?.SetTag("processed", processed);
                activity?.SetTag("outcome", outcome);
                var durationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                BackgroundServiceTelemetry.RecordRun(
                    "recordset-migration-dispatcher",
                    outcome,
                    processed,
                    durationMs);
            }

            try
            {
                var delay = job is null ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(1);
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }

        logger.LogInformation("RecordsetMigrationDispatcher: service stopped");
    }

    private static async Task<RecordsetMigrationJobDb?> GetNextJobAsync(
        RecordsetsDbContext dbContext,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        return await dbContext.RecordsetMigrationJobs
            .Where(j =>
                j.Stage == RecordsetMigrationJobStage.Pending &&
                (j.AvailableAfter == null || j.AvailableAfter <= now))
            .OrderBy(j => j.CreatedOn)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task RecoverStaleRunningJobsAsync(
        RecordsetsDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var recoveryCutoff = DateTime.UtcNow.Subtract(RunningRecoveryThreshold);
        var staleJobs = await dbContext.RecordsetMigrationJobs
            .Where(j =>
                j.Stage == RecordsetMigrationJobStage.Running &&
                j.StartedOn != null &&
                j.StartedOn <= recoveryCutoff)
            .OrderBy(j => j.StartedOn)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (staleJobs.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var job in staleJobs)
        {
            job.Stage = RecordsetMigrationJobStage.Pending;
            job.AvailableAfter = now;
            job.LastError = $"Recovered stale running job at {now:O}.";
            BackgroundServiceTelemetry.RecordRetryScheduled(
                "recordset-migration-dispatcher",
                "stale-running-recovery");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "RecordsetMigrationDispatcher: recovered {Count} stale running migration job(s)",
            staleJobs.Count);
    }

    private static async Task ProcessJobAsync(
        RecordsetMigrationJobDb job,
        RecordsetsDbContext dbContext,
        RecordsetMigrationJobRunner runner,
        ILogger logger,
        CancellationToken ct
    )
    {
        job.Stage = RecordsetMigrationJobStage.Running;
        job.StartedOn ??= DateTime.UtcNow;
        job.Attempts += 1;
        job.LastError = null;
        job.AvailableAfter = null;
        await dbContext.SaveChangesAsync(ct);

        try
        {
            await runner.RunAsync(job, ct);
            job.Stage = RecordsetMigrationJobStage.Completed;
            job.CompletedOn = DateTime.UtcNow;
            job.LastError = null;
            job.AvailableAfter = null;
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("RecordsetMigrationDispatcher: completed job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            if (job.Attempts >= MaxAttempts)
            {
                job.Stage = RecordsetMigrationJobStage.Failed;
                job.CompletedOn = DateTime.UtcNow;
                job.LastError = ex.Message;
                job.AvailableAfter = null;
                await dbContext.SaveChangesAsync(ct);

                logger.LogError(
                    ex,
                    "RecordsetMigrationDispatcher: job {JobId} reached max attempts ({Attempts}) and is now Failed",
                    job.Id,
                    job.Attempts);
                return;
            }

            var delay = ComputeRetryDelay(job.Attempts);
            job.Stage = RecordsetMigrationJobStage.Pending;
            job.LastError = ex.Message;
            job.AvailableAfter = DateTime.UtcNow.Add(delay);
            await dbContext.SaveChangesAsync(ct);
            BackgroundServiceTelemetry.RecordRetryScheduled(
                "recordset-migration-dispatcher",
                "job-failed");
            logger.LogWarning(ex,
                "RecordsetMigrationDispatcher: job {JobId} failed, will retry after {Delay}",
                job.Id,
                delay);
        }
    }

    private static async Task CleanupExpiredBackupsAsync(
        RecordsetsDbContext dbContext,
        IRecordsetsUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        var expiredJobs = await dbContext.RecordsetMigrationJobs
            .Where(j =>
                j.Stage == RecordsetMigrationJobStage.Completed &&
                j.BackupRecordsetId != null &&
                j.BackupRemovedOn == null &&
                j.BackupExpiresOn != null &&
                j.BackupExpiresOn <= now)
            .OrderBy(j => j.BackupExpiresOn)
            .Take(5)
            .ToListAsync(ct);

        foreach (var job in expiredJobs)
        {
            try
            {
                if (job.BackupRecordsetId is null)
                {
                    job.BackupRemovedOn = DateTime.UtcNow;
                    job.Stage = RecordsetMigrationJobStage.Archived;
                    await unitOfWork.SaveChangesAsync(ct);
                    continue;
                }

                await unitOfWork.DeleteRecordsetAsync(UlidId.Parse(job.BackupRecordsetId), ct);
                job.BackupRemovedOn = DateTime.UtcNow;
                job.Stage = RecordsetMigrationJobStage.Archived;
                await unitOfWork.SaveChangesAsync(ct);

                logger.LogInformation(
                    "RecordsetMigrationDispatcher: removed backup recordset {BackupRecordsetId} for job {JobId}",
                    job.BackupRecordsetId,
                    job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "RecordsetMigrationDispatcher: failed to remove backup recordset for job {JobId}",
                    job.Id);
            }
        }
    }

    private static TimeSpan ComputeRetryDelay(int attempts)
    {
        var seconds = Math.Min(Math.Pow(2, attempts), 600);
        return TimeSpan.FromSeconds(seconds);
    }
}