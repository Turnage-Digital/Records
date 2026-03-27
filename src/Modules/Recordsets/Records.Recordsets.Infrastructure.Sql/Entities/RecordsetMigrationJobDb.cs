using Records.Recordsets.Domain;

namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetMigrationJobDb
{
    public string Id { get; set; } = string.Empty;
    public string SourceRecordsetId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string PlanJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? BackupRemovedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? AvailableAfter { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? BackupRecordsetId { get; set; }
    public string? NewRecordsetId { get; set; }
    public DateTime? BackupExpiresAt { get; set; }
    public RecordsetMigrationJobStage Stage { get; set; }
}