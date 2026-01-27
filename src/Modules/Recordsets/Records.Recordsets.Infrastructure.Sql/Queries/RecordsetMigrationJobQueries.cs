using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Infrastructure.Sql.Queries;

public sealed class RecordsetMigrationJobQueries(RecordsetsDbContext dbContext) : IRecordsetMigrationJobQueries
{
    public async Task<RecordsetMigrationJobStatusDto?> GetAsync(
        UlidId recordsetId,
        UlidId correlationId,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var correlationKey = correlationId.ToString();
        return await dbContext.RecordsetMigrationJobs
            .AsNoTracking()
            .Where(j => j.SourceRecordsetId == recordsetKey && j.CorrelationId == correlationKey)
            .Select(j => new RecordsetMigrationJobStatusDto(
                UlidId.Parse(j.Id),
                UlidId.Parse(j.SourceRecordsetId),
                UlidId.Parse(j.CorrelationId),
                j.Stage,
                UlidId.Parse(j.RequestedBy),
                j.CreatedOn,
                j.StartedOn,
                j.CompletedOn,
                j.BackupRecordsetId == null ? null : UlidId.Parse(j.BackupRecordsetId),
                j.NewRecordsetId == null ? null : UlidId.Parse(j.NewRecordsetId),
                j.BackupExpiresOn,
                j.BackupRemovedOn,
                j.Attempts,
                j.LastError))
            .FirstOrDefaultAsync(cancellationToken);
    }
}