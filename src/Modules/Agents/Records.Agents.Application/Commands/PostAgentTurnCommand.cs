using MediatR;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Application.Commands;

public sealed record PostAgentTurnCommand(string ThreadId, string Message, string? PastedText = null)
    : IRequest<AgentThreadDto?>;

public sealed class PostAgentTurnCommandHandler(
    IAgentConversationService conversationService
) : IRequestHandler<PostAgentTurnCommand, AgentThreadDto?>
{
    public Task<AgentThreadDto?> Handle(PostAgentTurnCommand request, CancellationToken cancellationToken)
    {
        return conversationService.PostTurnAsync(
            request.ThreadId,
            request.Message,
            request.PastedText,
            cancellationToken);
    }
}
