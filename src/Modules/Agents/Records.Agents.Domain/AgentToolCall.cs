namespace Records.Agents.Domain;

public sealed class AgentToolCall
{
    public AgentToolCall(
        string id,
        string threadId,
        string turnId,
        string name,
        string argumentsJson,
        string status,
        string? summary,
        string? error,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt
    )
    {
        Id = id;
        ThreadId = threadId;
        TurnId = turnId;
        Name = name;
        ArgumentsJson = argumentsJson;
        Status = status;
        Summary = summary;
        Error = error;
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    public string Id { get; }
    public string ThreadId { get; }
    public string TurnId { get; }
    public string Name { get; }
    public string ArgumentsJson { get; }
    public string Status { get; }
    public string? Summary { get; }
    public string? Error { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; }
}
