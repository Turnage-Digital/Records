using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Domain;

public sealed class AgentProposal
{
    public AgentProposal(
        UlidId id,
        UlidId threadId,
        ProposalState state,
        string payloadJson,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt
    )
    {
        Id = id;
        ThreadId = threadId;
        State = state;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public UlidId Id { get; }
    public UlidId ThreadId { get; }
    public ProposalState State { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }

    public void Confirm()
    {
        State = ProposalState.Confirmed;
    }

    public void Reject()
    {
        State = ProposalState.Rejected;
    }

    public void ExpireIfNeeded(DateTimeOffset now)
    {
        if (State == ProposalState.Pending && now >= ExpiresAt)
        {
            State = ProposalState.Expired;
        }
    }

    public void ReplacePayload(string payloadJson)
    {
        PayloadJson = payloadJson;
    }
}
