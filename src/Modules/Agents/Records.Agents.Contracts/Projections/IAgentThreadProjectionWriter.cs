namespace Records.Agents.Contracts.Projections;

public interface IAgentThreadProjectionWriter
{
    Task UpsertAsync(AgentThreadProjectionModel model, CancellationToken cancellationToken);
}

public sealed record AgentThreadProjectionModel(
    string Id,
    string UserId,
    string? TenantId,
    string Title,
    DateTimeOffset UpdatedAt
);
