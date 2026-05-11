using MediatR;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Application.Commands;

public sealed record RejectAgentProposalCommand(string ThreadId, string ProposalId) : IRequest<AgentThreadDto?>;

public sealed class RejectAgentProposalCommandHandler(
    IAgentConversationService conversationService
) : IRequestHandler<RejectAgentProposalCommand, AgentThreadDto?>
{
    public Task<AgentThreadDto?> Handle(
        RejectAgentProposalCommand request,
        CancellationToken cancellationToken
    )
    {
        return conversationService.RejectProposalAsync(
            request.ThreadId,
            request.ProposalId,
            cancellationToken);
    }
}
