namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetDb
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public List<RecordsetColumnDb> Columns { get; set; } = [];
    public List<RecordsetStatusDb> Statuses { get; set; } = [];
    public List<RecordsetStatusTransitionDb> StatusTransitions { get; set; } = [];
}