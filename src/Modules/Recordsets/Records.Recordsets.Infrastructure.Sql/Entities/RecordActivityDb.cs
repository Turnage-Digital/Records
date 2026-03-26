namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordActivityDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public long RecordId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? BagJson { get; set; }
    public RecordDb? Record { get; set; }
}
