using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.App.Server.Services;

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
        recordset.Rename(backupName, requestedBy, DateTimeOffset.UtcNow);
        await unitOfWork.UpdateRecordsetAsync(recordset, ct);
        await unitOfWork.SaveChangesAsync(ct);

        job.BackupRecordsetId = recordset.Id.ToString();
        job.BackupExpiresOn ??= DateTime.UtcNow.Add(DefaultBackupRetention);

        var backupCount = await unitOfWork.GetRecordCountAsync(recordset.Id, ct);
        await projectionWriter.UpsertAsync(
            new RecordsetProjectionModel(recordset.Id, backupName, backupCount, DateTimeOffset.UtcNow),
            ct);

        var newRecordset = Recordset.Create(
            UlidId.NewUlid(),
            originalName,
            requestedBy,
            DateTimeOffset.UtcNow,
            context.Columns,
            context.Statuses,
            context.StatusTransitions);

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
                    transformedBag,
                    requestedBy,
                    DateTimeOffset.UtcNow);

                await unitOfWork.AddRecordAsync(newRecord, ct);
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