namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentThreadDb
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string BackendId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
