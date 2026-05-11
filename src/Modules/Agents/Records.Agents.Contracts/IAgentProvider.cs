using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts;

public interface IAgentProvider
{
    Task<AgentTurnResultDto> ExecuteTurnAsync(
        AgentProviderContextDto context,
        Func<AgentStreamEventDto, CancellationToken, Task>? onProgress,
        CancellationToken cancellationToken
    );
}
