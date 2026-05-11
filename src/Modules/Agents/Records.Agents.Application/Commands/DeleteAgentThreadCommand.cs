using MediatR;
using Records.Agents.Contracts;

namespace Records.Agents.Application.Commands;

public sealed record DeleteAgentThreadCommand(string ThreadId) : IRequest<bool>;

public sealed class DeleteAgentThreadCommandHandler(
    IAgentConversationService conversationService
) : IRequestHandler<DeleteAgentThreadCommand, bool>
{
    public Task<bool> Handle(DeleteAgentThreadCommand request, CancellationToken cancellationToken)
    {
        return conversationService.DeleteThreadAsync(request.ThreadId, cancellationToken);
    }
}
