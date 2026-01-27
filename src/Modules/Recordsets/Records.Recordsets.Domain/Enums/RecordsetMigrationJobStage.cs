namespace Records.Recordsets.Domain.Enums;

public enum RecordsetMigrationJobStage
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Archived = 4
}