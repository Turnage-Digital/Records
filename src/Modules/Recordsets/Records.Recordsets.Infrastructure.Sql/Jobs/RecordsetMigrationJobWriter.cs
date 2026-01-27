using Records.Recordsets.Contracts.Jobs;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Jobs;

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
            CreatedOn = model.CreatedOn,
            Stage = model.Stage,
            CorrelationId = model.CorrelationId.ToString()
        };

        dbContext.RecordsetMigrationJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}