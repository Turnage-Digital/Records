namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public string BagJson { get; set; } = string.Empty;
}
