namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetDb
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<RecordsetColumnDb> Columns { get; set; } = [];
    public List<RecordsetStatusDb> Statuses { get; set; } = [];
    public List<RecordsetStatusTransitionDb> StatusTransitions { get; set; } = [];
}