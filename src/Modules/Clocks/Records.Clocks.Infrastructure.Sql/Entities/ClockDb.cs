namespace Records.Clocks.Infrastructure.Sql.Entities;

public sealed class ClockDb
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string RecordsetId { get; set; } = null!;
    public int RecordId { get; set; }
    public string DefinitionId { get; set; } = null!;
    public int State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime AtRiskDueAt { get; set; }
    public DateTime BreachDueAt { get; set; }
    public DateTime? AtRiskAt { get; set; }
    public DateTime? BreachedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PauseReason { get; set; }
    public long AccumulatedPauseMs { get; set; }
}