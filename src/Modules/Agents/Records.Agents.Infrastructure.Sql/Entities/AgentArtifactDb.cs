namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentArtifactDb
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
