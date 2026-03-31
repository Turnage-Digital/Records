namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentThreadProjectionDb
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
