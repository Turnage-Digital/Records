using Microsoft.EntityFrameworkCore;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Contracts.Queries;
using Records.Core.Contracts;
using Records.Agents.Infrastructure.Sql.Entities;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentThreadQueries(
    AgentsDbContext dbContext,
    ICurrentUserAccess currentUserAccess,
    ITenantContext tenantContext
) : IAgentThreadQueries
{
    public async Task<IReadOnlyList<AgentThreadSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        if (!currentUserAccess.TryGetCurrentUserId(out var userId))
        {
            return [];
        }

        return await CreateCurrentUserThreadQuery(userId.ToString())
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AgentThreadSummaryDto
            {
                Id = x.Id,
                Title = x.Title,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentThreadDto?> GetByIdAsync(string threadId, CancellationToken cancellationToken)
    {
        if (!currentUserAccess.TryGetCurrentUserId(out var userId))
        {
            return null;
        }

        var thread = await CreateCurrentUserThreadQuery(userId.ToString())
            .FirstOrDefaultAsync(x => x.Id == threadId, cancellationToken);

        if (thread is null)
        {
            return null;
        }

        var turns = await dbContext.AgentTurns
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .OrderBy(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);

        var toolCalls = await dbContext.AgentToolCalls
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .OrderBy(x => x.StartedAt)
            .ToArrayAsync(cancellationToken);

        var artifactDb = await dbContext.AgentArtifacts
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId && x.IsCurrent)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var proposalDb = await dbContext.AgentProposals
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId && x.IsCurrent)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        WorkspaceProposalDto? pendingProposal = null;
        if (proposalDb is not null && proposalDb.ExpiresAt > DateTimeOffset.UtcNow)
        {
            pendingProposal = AgentJsonSerializer.Deserialize<WorkspaceProposalDto>(proposalDb.PayloadJson);
        }

        return new AgentThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            UpdatedAt = thread.UpdatedAt,
            Turns = turns
                .Select(x => new AgentTurnDto
                {
                    Id = x.Id,
                    Role = x.Role,
                    Content = x.Content,
                    PastedText = x.PastedText,
                    CreatedAt = x.CreatedAt,
                    ToolCalls = toolCalls
                        .Where(toolCall => toolCall.TurnId == x.Id)
                        .Select(toolCall => new AgentToolCallDto
                        {
                            Id = toolCall.Id,
                            Name = toolCall.Name,
                            ArgumentsJson = toolCall.ArgumentsJson,
                            Status = toolCall.Status,
                            Summary = toolCall.Summary,
                            Error = toolCall.Error,
                            StartedAt = toolCall.StartedAt,
                            CompletedAt = toolCall.CompletedAt
                        })
                        .ToArray()
                })
                .ToArray(),
            CurrentArtifact = artifactDb is null
                ? null
                : AgentJsonSerializer.Deserialize<WorkspaceArtifactDto>(artifactDb.PayloadJson),
            PendingProposal = pendingProposal
        };
    }

    internal IQueryable<AgentThreadProjectionDb> CreateCurrentUserThreadQuery(string userId)
    {
        var tenantId = AgentTenantId.Normalize(tenantContext.TenantId);
        var query = dbContext.AgentThreadProjections
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (tenantId is null)
        {
            return query.Where(x => x.TenantId == null || x.TenantId.Trim() == string.Empty);
        }

        return query.Where(x => x.TenantId == tenantId);
    }
}
