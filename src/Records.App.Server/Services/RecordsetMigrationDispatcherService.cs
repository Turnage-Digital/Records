using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Enums;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.App.Server.Services;

public class RecordsetMigrationDispatcherService(
    ILogger<RecordsetMigrationDispatcherService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RecordsetMigrationDispatcher: service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            RecordsetMigrationJobDb? job = null;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RecordsetsDbContext>();
                var runner = scope.ServiceProvider.GetRequiredService<RecordsetMigrationJobRunner>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IRecordsetsUnitOfWork>();
                job = await GetNextJobAsync(dbContext, stoppingToken);
                if (job is null)
                {
                    logger.LogDebug("RecordsetMigrationDispatcher: no pending jobs");
                }
                else
                {
                    await ProcessJobAsync(job, dbContext, runner, logger, stoppingToken);
                }

                await CleanupExpiredBackupsAsync(dbContext, unitOfWork, logger, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "RecordsetMigrationDispatcher: loop error");
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
                (j.Stage == RecordsetMigrationJobStage.Pending || j.Stage == RecordsetMigrationJobStage.Failed) &&
                (j.AvailableAfter == null || j.AvailableAfter <= now))
            .OrderBy(j => j.CreatedOn)
            .FirstOrDefaultAsync(ct);
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
            var delay = ComputeRetryDelay(job.Attempts);
            job.Stage = RecordsetMigrationJobStage.Pending;
            job.LastError = ex.Message;
            job.AvailableAfter = DateTime.UtcNow.Add(delay);
            await dbContext.SaveChangesAsync(ct);
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
        var seconds = Math.Min(Math.Pow(2, attempts), 300);
        return TimeSpan.FromSeconds(seconds);
    }
}