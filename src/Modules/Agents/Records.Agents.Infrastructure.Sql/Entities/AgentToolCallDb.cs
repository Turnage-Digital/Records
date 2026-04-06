namespace Records.Agents.Infrastructure.Sql.Entities;

public sealed class AgentToolCallDb
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
