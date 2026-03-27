using Records.Recordsets.Domain;

namespace Records.Recordsets.Infrastructure.Sql.Entities;

public class RecordsetColumnDb
{
    public long Id { get; set; }
    public string RecordsetId { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ColumnType Type { get; set; }
    public bool Required { get; set; }
    public string? AllowedValuesJson { get; set; }
    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }
    public string? Regex { get; set; }
}