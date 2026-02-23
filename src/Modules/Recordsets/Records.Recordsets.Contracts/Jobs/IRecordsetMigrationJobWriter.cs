using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Contracts.Jobs;

public interface IRecordsetMigrationJobWriter
{
    Task CreateAsync(RecordsetMigrationJobWriteModel model, CancellationToken cancellationToken);
}

public sealed record RecordsetMigrationJobWriteModel(
    UlidId JobId,
    UlidId SourceRecordsetId,
    UlidId CorrelationId,
    UlidId RequestedBy,
    string PlanJson,
    DateTime CreatedOn,
    RecordsetMigrationJobStage Stage
);