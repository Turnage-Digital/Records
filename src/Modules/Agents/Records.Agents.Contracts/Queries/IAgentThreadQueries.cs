using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Contracts.Queries;

public interface IAgentThreadQueries
{
    Task<IReadOnlyList<AgentThreadSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<AgentThreadDto?> GetByIdAsync(string threadId, CancellationToken cancellationToken);
}
