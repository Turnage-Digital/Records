using Records.Recordsets.Domain.Enums;

namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetMigrationJobDb
{
    public string Id { get; set; } = string.Empty;
    public string SourceRecordsetId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string PlanJson { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public DateTime? BackupRemovedOn { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? AvailableAfter { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? BackupRecordsetId { get; set; }
    public string? NewRecordsetId { get; set; }
    public DateTime? BackupExpiresOn { get; set; }
    public RecordsetMigrationJobStage Stage { get; set; }
}