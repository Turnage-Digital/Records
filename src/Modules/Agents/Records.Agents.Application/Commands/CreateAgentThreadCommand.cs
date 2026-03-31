using MediatR;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Application.Commands;

public sealed record CreateAgentThreadCommand(string? Title = null)
    : IRequest<AgentThreadSummaryDto>;

public sealed class CreateAgentThreadCommandHandler(
    IAgentConversationService conversationService
) : IRequestHandler<CreateAgentThreadCommand, AgentThreadSummaryDto>
{
    public Task<AgentThreadSummaryDto> Handle(
        CreateAgentThreadCommand request,
        CancellationToken cancellationToken
    )
    {
        return conversationService.CreateThreadAsync(request.Title, cancellationToken);
    }
}
