namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public string BagJson { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}