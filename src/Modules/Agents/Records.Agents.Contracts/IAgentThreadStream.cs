using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts;

public interface IAgentThreadStream
{
    IAsyncEnumerable<AgentStreamEventDto> SubscribeAsync(string threadId, CancellationToken cancellationToken);

    ValueTask PublishAsync(
        string threadId,
        AgentStreamEventDto streamEvent,
        CancellationToken cancellationToken = default
    );
}
