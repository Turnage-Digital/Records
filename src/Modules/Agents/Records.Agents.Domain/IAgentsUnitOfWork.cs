using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Domain;

public interface IAgentsUnitOfWork : IUnitOfWork
{
    Task<AgentThread?> GetThreadByIdAsync(UlidId threadId, CancellationToken cancellationToken);
    Task AddThreadAsync(AgentThread thread, CancellationToken cancellationToken);
    Task UpdateThreadAsync(AgentThread thread, CancellationToken cancellationToken);
    Task AddTurnAsync(AgentTurn turn, CancellationToken cancellationToken);
    Task AddToolCallsAsync(IEnumerable<AgentToolCall> toolCalls, CancellationToken cancellationToken);
    Task ReplaceCurrentArtifactAsync(AgentArtifact artifact, CancellationToken cancellationToken);
    Task<AgentProposal?> GetProposalByIdAsync(UlidId proposalId, CancellationToken cancellationToken);
    Task ReplacePendingProposalAsync(AgentProposal proposal, CancellationToken cancellationToken);
}
