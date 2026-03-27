namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetStatusTransitionDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string AllowedNextJson { get; set; } = string.Empty;
}