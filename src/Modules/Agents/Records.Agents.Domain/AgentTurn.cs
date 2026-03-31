using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Domain;

public sealed class AgentTurn
{
    public AgentTurn(
        UlidId id,
        UlidId threadId,
        AgentTurnRole role,
        string content,
        string? pastedText,
        DateTimeOffset createdAt
    )
    {
        Id = id;
        ThreadId = threadId;
        Role = role;
        Content = content;
        PastedText = pastedText;
        CreatedAt = createdAt;
    }

    public UlidId Id { get; }
    public UlidId ThreadId { get; }
    public AgentTurnRole Role { get; }
    public string Content { get; }
    public string? PastedText { get; }
    public DateTimeOffset CreatedAt { get; }
}
