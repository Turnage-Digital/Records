using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Projections;
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
            .OrderBy(j => j.CreatedAt)
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
                j.StartedAt != null &&
                j.StartedAt <= recoveryCutoff)
            .OrderBy(j => j.StartedAt)
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
        job.StartedAt ??= DateTime.UtcNow;
        job.Attempts += 1;
        job.LastError = null;
        job.AvailableAfter = null;
        await dbContext.SaveChangesAsync(ct);

        try
        {
            await runner.RunAsync(job, ct);
            job.Stage = RecordsetMigrationJobStage.Completed;
            job.CompletedAt = DateTime.UtcNow;
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
                job.CompletedAt = DateTime.UtcNow;
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
                j.BackupRemovedAt == null &&
                j.BackupExpiresAt != null &&
                j.BackupExpiresAt <= now)
            .OrderBy(j => j.BackupExpiresAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var job in expiredJobs)
        {
            try
            {
                if (job.BackupRecordsetId is null)
                {
                    job.BackupRemovedAt = DateTime.UtcNow;
                    job.Stage = RecordsetMigrationJobStage.Archived;
                    await unitOfWork.SaveChangesAsync(ct);
                    continue;
                }

                await unitOfWork.DeleteRecordsetAsync(UlidId.Parse(job.BackupRecordsetId), ct);
                job.BackupRemovedAt = DateTime.UtcNow;
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

public class RecordsetMigrationJobRunner(
    IRecordsetsUnitOfWork unitOfWork,
    RecordsetsDbContext dbContext,
    IRecordsetProjectionWriter projectionWriter,
    ILogger<RecordsetMigrationJobRunner> logger
)
{
    private const int BatchSize = 250;
    private static readonly JsonSerializerOptions PlanSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultBackupRetention = TimeSpan.FromDays(7);

    public async Task RunAsync(RecordsetMigrationJobDb job, CancellationToken cancellationToken)
    {
        MigrationPlan plan;
        try
        {
            plan = JsonSerializer.Deserialize<MigrationPlan>(job.PlanJson, PlanSerializerOptions) ??
                   throw new InvalidOperationException("Migration plan payload could not be deserialized.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid migration plan payload.", ex);
        }

        try
        {
            await ExecuteAsync(job, plan, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "RecordsetMigrationJobRunner: job {JobId} failed for recordset {RecordsetId}",
                job.Id,
                job.SourceRecordsetId);
            throw;
        }
    }

    private async Task ExecuteAsync(RecordsetMigrationJobDb job, MigrationPlan plan, CancellationToken ct)
    {
        var sourceId = UlidId.Parse(job.SourceRecordsetId);
        var recordset = await unitOfWork.GetRecordsetByIdAsync(sourceId, ct)
                        ?? throw new InvalidOperationException(
                            $"Source recordset {job.SourceRecordsetId} not found.");

        var originalName = recordset.Name;
        var context = MigrationPlanApplier.Prepare(
            plan,
            recordset.Columns,
            recordset.Statuses,
            recordset.StatusTransitions);

        var backupName = await BuildUniqueBackupNameAsync(originalName, ct);
        var requestedBy = UlidId.Parse(job.RequestedBy);
        recordset.Rename(backupName, DateTimeOffset.UtcNow);
        await unitOfWork.UpdateRecordsetAsync(recordset, ct);
        await unitOfWork.SaveChangesAsync(ct);

        job.BackupRecordsetId = recordset.Id.ToString();
        job.BackupExpiresAt ??= DateTime.UtcNow.Add(DefaultBackupRetention);

        var backupCount = await unitOfWork.GetRecordCountAsync(recordset.Id, ct);
        await projectionWriter.UpsertAsync(
            new RecordsetProjectionModel(recordset.Id, backupName, backupCount, DateTimeOffset.UtcNow),
            ct);

        var newRecordset = Recordset.Create(
            UlidId.NewUlid(),
            originalName,
            context.Columns,
            context.Statuses,
            context.StatusTransitions,
            DateTimeOffset.UtcNow);

        await unitOfWork.AddRecordsetAsync(newRecordset, ct);
        await unitOfWork.SaveChangesAsync(ct);

        job.NewRecordsetId = newRecordset.Id.ToString();

        await projectionWriter.UpsertAsync(
            new RecordsetProjectionModel(newRecordset.Id, originalName, 0, DateTimeOffset.UtcNow),
            ct);

        var totalRecords = await dbContext.RecordsetItems
            .AsNoTracking()
            .Where(i => i.RecordsetId == job.SourceRecordsetId)
            .CountAsync(ct);

        var processed = await CopyRecordsAsync(job, newRecordset, context, requestedBy, totalRecords, ct);

        await projectionWriter.UpdateItemCountAsync(newRecordset.Id, processed, ct);

        logger.LogInformation(
            "RecordsetMigrationJobRunner: completed job {JobId} ({Processed} records)",
            job.Id,
            processed);
    }

    private async Task<int> CopyRecordsAsync(
        RecordsetMigrationJobDb job,
        Recordset newRecordset,
        MigrationPlanContext context,
        UlidId requestedBy,
        int totalRecords,
        CancellationToken ct
    )
    {
        var processed = 0;
        var lastReportedPercent = 20;
        long lastRecordId = 0;

        while (true)
        {
            var batch = await dbContext.RecordsetItems
                .AsNoTracking()
                .Where(i => i.RecordsetId == job.SourceRecordsetId && i.Id > lastRecordId)
                .OrderBy(i => i.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var sourceRecord in batch)
            {
                var bag = JsonSerializer.Deserialize<object>(sourceRecord.BagJson) ?? new object();
                var transformedBag = MigrationPlanApplier.ApplyToRecord(context, bag);
                var newRecord = new Record(
                    0,
                    newRecordset.Id,
                    transformedBag);

                await unitOfWork.AddRecordAsync(newRecord, requestedBy, DateTimeOffset.UtcNow, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            processed += batch.Count;
            lastRecordId = batch[^1].Id;

            var percent = ComputeProgressPercentage(processed, totalRecords);
            if (percent > lastReportedPercent)
            {
                lastReportedPercent = percent;
                logger.LogInformation(
                    "RecordsetMigrationJobRunner: job {JobId} progress {Percent}% ({Processed}/{Total})",
                    job.Id,
                    percent,
                    processed,
                    totalRecords);
            }
        }

        if (totalRecords == 0)
        {
            logger.LogInformation(
                "RecordsetMigrationJobRunner: job {JobId} has no records to copy",
                job.Id);
        }

        return processed;
    }

    private static int ComputeProgressPercentage(int processed, int total)
    {
        if (total == 0)
        {
            return 95;
        }

        var percent = 20 + (int)Math.Round(70.0 * processed / total);
        if (percent > 95)
        {
            percent = 95;
        }

        return percent;
    }

    private async Task<string> BuildUniqueBackupNameAsync(string originalName, CancellationToken ct)
    {
        const int maxLength = 256;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = attempt == 0 ? $" (backup {timestamp})" : $" (backup {timestamp}-{attempt})";
            var maxBaseLength = Math.Max(1, maxLength - suffix.Length);
            var baseName = originalName.Length > maxBaseLength
                ? originalName[..maxBaseLength]
                : originalName;
            var candidate = $"{baseName}{suffix}";
            var exists = await dbContext.Recordsets.AnyAsync(l => l.Name == candidate, ct);
            if (!exists)
            {
                return candidate;
            }
        }

        for (;;)
        {
            var candidate = $"{Guid.NewGuid():N}".Substring(0, 8);
            var backupName = $"{candidate} (backup {timestamp})";
            if (backupName.Length > maxLength)
            {
                backupName = backupName[..maxLength];
            }

            var exists = await dbContext.Recordsets.AnyAsync(l => l.Name == backupName, ct);
            if (!exists)
            {
                return backupName;
            }
        }
    }
}