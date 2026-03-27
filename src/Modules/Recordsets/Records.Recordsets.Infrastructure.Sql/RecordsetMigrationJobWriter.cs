using Records.Recordsets.Contracts;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetMigrationJobWriter(RecordsetsDbContext dbContext) : IRecordsetMigrationJobWriter
{
    public async Task CreateAsync(RecordsetMigrationJobWriteModel model, CancellationToken cancellationToken)
    {
        var job = new RecordsetMigrationJobDb
        {
            Id = model.JobId.ToString(),
            SourceRecordsetId = model.SourceRecordsetId.ToString(),
            RequestedBy = model.RequestedBy.ToString(),
            PlanJson = model.PlanJson,
            CreatedAt = model.CreatedAt,
            Stage = model.Stage,
            CorrelationId = model.CorrelationId.ToString()
        };

        dbContext.RecordsetMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}