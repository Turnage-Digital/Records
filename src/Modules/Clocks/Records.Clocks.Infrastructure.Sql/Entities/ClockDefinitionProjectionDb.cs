namespace Records.Clocks.Infrastructure.Sql.Entities;

public sealed class ClockDefinitionProjectionDb
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AtRiskThresholdValue { get; set; }
    public int AtRiskThresholdUnit { get; set; }
    public int BreachThresholdValue { get; set; }
    public int BreachThresholdUnit { get; set; }
    public bool IsActive { get; set; }
}