using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts;

public interface IAgentConversationService
{
    Task<AgentThreadSummaryDto> CreateThreadAsync(string? title, CancellationToken cancellationToken);

    Task<AgentThreadDto?> PostTurnAsync(
        string threadId,
        string message,
        string? pastedText,
        CancellationToken cancellationToken
    );

    Task<AgentThreadDto?> ConfirmProposalAsync(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    );

    Task<AgentThreadDto?> RejectProposalAsync(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    );
}
