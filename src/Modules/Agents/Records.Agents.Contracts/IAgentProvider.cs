using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts;

public interface IAgentProvider
{
    Task<AgentTurnResultDto> ExecuteTurnAsync(
        AgentProviderContextDto context,
        IWorkspaceBackendAdapter backendAdapter,
        CancellationToken cancellationToken
    );
}
