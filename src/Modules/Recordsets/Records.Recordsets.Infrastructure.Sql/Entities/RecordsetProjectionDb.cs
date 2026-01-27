namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetProjectionDb
{
    public string RecordsetId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}