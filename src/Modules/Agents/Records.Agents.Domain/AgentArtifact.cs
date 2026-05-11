using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Domain;

public sealed class AgentArtifact
{
    public AgentArtifact(
        UlidId id,
        UlidId threadId,
        string kind,
        string payloadJson,
        DateTimeOffset createdAt
    )
    {
        Id = id;
        ThreadId = threadId;
        Kind = kind;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
    }

    public UlidId Id { get; }
    public UlidId ThreadId { get; }
    public string Kind { get; }
    public string PayloadJson { get; }
    public DateTimeOffset CreatedAt { get; }
}
