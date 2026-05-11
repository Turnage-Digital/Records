using MediatR;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Application.Commands;

public sealed record ConfirmAgentProposalCommand(string ThreadId, string ProposalId) : IRequest<AgentThreadDto?>;

public sealed class ConfirmAgentProposalCommandHandler(
    IAgentConversationService conversationService
) : IRequestHandler<ConfirmAgentProposalCommand, AgentThreadDto?>
{
    public Task<AgentThreadDto?> Handle(
        ConfirmAgentProposalCommand request,
        CancellationToken cancellationToken
    )
    {
        return conversationService.ConfirmProposalAsync(
            request.ThreadId,
            request.ProposalId,
            cancellationToken);
    }
}
