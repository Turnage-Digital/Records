using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetMigrationJobStatusDto(
    UlidId JobId,
    UlidId SourceRecordsetId,
    UlidId CorrelationId,
    RecordsetMigrationJobStage Stage,
    UlidId RequestedBy,
    DateTime CreatedOn,
    DateTime? StartedOn,
    DateTime? CompletedOn,
    UlidId? BackupRecordsetId,
    UlidId? NewRecordsetId,
    DateTime? BackupExpiresOn,
    DateTime? BackupRemovedOn,
    int Attempts,
    string? LastError
);