namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentProposalDb
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public int State { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
