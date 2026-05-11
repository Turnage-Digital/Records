namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentTurnDb
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? PastedText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
