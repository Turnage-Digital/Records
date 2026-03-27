using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetMigrationJobStatusDto(
    UlidId JobId,
    UlidId SourceRecordsetId,
    UlidId CorrelationId,
    RecordsetMigrationJobStage Stage,
    UlidId RequestedBy,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    UlidId? BackupRecordsetId,
    UlidId? NewRecordsetId,
    DateTime? BackupExpiresAt,
    DateTime? BackupRemovedAt,
    int Attempts,
    string? LastError
);