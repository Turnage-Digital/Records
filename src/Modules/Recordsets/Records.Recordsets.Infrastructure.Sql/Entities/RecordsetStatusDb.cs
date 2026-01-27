namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetStatusDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}