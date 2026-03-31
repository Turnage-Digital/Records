using Microsoft.EntityFrameworkCore;
using Records.Agents.Contracts.Projections;
using Records.Agents.Infrastructure.Sql.Entities;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentThreadProjectionWriter(AgentsDbContext dbContext) : IAgentThreadProjectionWriter
{
    public async Task UpsertAsync(AgentThreadProjectionModel model, CancellationToken cancellationToken)
    {
        var projection = await dbContext.AgentThreadProjections
            .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

        if (projection is null)
        {
            projection = new AgentThreadProjectionDb
            {
                Id = model.Id,
                UserId = model.UserId,
                TenantId = AgentTenantId.Normalize(model.TenantId),
                Title = model.Title,
                UpdatedAt = model.UpdatedAt
            };
            dbContext.AgentThreadProjections.Add(projection);
        }
        else
        {
            projection.UserId = model.UserId;
            projection.TenantId = AgentTenantId.Normalize(model.TenantId);
            projection.Title = model.Title;
            projection.UpdatedAt = model.UpdatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
