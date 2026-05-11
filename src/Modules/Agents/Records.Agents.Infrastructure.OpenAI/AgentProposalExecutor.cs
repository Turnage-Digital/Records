using System.Diagnostics;
using Records.Agents.Contracts.Dtos;
using Records.Core.Contracts;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class AgentProposalExecutor(
    IAgentBackendRegistry backendRegistry,
    ITenantContext tenantContext
) : IAgentProposalExecutor
{
    public async Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        string backendId,
        WorkspaceProposalDto proposal,
        CancellationToken cancellationToken
    )
    {
        return await backendRegistry.GetRequiredBackend(backendId).ApplyConfirmedProposalAsync(
            proposal,
            new AgentMcpCallContext(
                tenantContext.TenantId,
                tenantContext.ActorId,
                Activity.Current?.Id),
            cancellationToken);
    }
}
