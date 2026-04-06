using MediatR;
using Microsoft.EntityFrameworkCore;
using Records.Agents.Domain;
using Records.Agents.Infrastructure.Sql.Entities;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentsUnitOfWork : UnitOfWork<AgentsDbContext>, IAgentsUnitOfWork
{
    private readonly AgentsDbContext _dbContext;

    public AgentsUnitOfWork(
        AgentsDbContext dbContext,
        IMediator mediator
    ) : base(dbContext, mediator)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentThread?> GetThreadByIdAsync(UlidId threadId, CancellationToken cancellationToken)
    {
        var thread = await _dbContext.AgentThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == threadId.ToString(), cancellationToken);

        return thread is null ? null : MapThread(thread);
    }

    public async Task AddThreadAsync(AgentThread thread, CancellationToken cancellationToken)
    {
        await _dbContext.AgentThreads.AddAsync(new AgentThreadDb
        {
            Id = thread.Id.ToString(),
            UserId = thread.UserId,
            TenantId = AgentTenantId.Normalize(thread.TenantId),
            BackendId = thread.BackendId,
            Title = thread.Title,
            CreatedAt = thread.CreatedAt,
            UpdatedAt = thread.UpdatedAt
        }, cancellationToken);
    }

    public Task UpdateThreadAsync(AgentThread thread, CancellationToken cancellationToken)
    {
        var key = thread.Id.ToString();
        var existing = _dbContext.AgentThreads.Local.FirstOrDefault(x => x.Id == key);
        if (existing is not null)
        {
            existing.UserId = thread.UserId;
            existing.TenantId = AgentTenantId.Normalize(thread.TenantId);
            existing.BackendId = thread.BackendId;
            existing.Title = thread.Title;
            existing.CreatedAt = thread.CreatedAt;
            existing.UpdatedAt = thread.UpdatedAt;
            return Task.CompletedTask;
        }

        _dbContext.AgentThreads.Update(new AgentThreadDb
        {
            Id = key,
            UserId = thread.UserId,
            TenantId = AgentTenantId.Normalize(thread.TenantId),
            BackendId = thread.BackendId,
            Title = thread.Title,
            CreatedAt = thread.CreatedAt,
            UpdatedAt = thread.UpdatedAt
        });
        return Task.CompletedTask;
    }

    public async Task AddTurnAsync(AgentTurn turn, CancellationToken cancellationToken)
    {
        await _dbContext.AgentTurns.AddAsync(new AgentTurnDb
        {
            Id = turn.Id.ToString(),
            ThreadId = turn.ThreadId.ToString(),
            Role = turn.Role.ToString().ToLowerInvariant(),
            Content = turn.Content,
            PastedText = turn.PastedText,
            CreatedAt = turn.CreatedAt
        }, cancellationToken);
    }

    public async Task AddToolCallsAsync(IEnumerable<AgentToolCall> toolCalls, CancellationToken cancellationToken)
    {
        var rows = toolCalls
            .Select(toolCall => new AgentToolCallDb
            {
                Id = toolCall.Id,
                ThreadId = toolCall.ThreadId,
                TurnId = toolCall.TurnId,
                Name = toolCall.Name,
                ArgumentsJson = toolCall.ArgumentsJson,
                Status = toolCall.Status,
                Summary = toolCall.Summary,
                Error = toolCall.Error,
                StartedAt = toolCall.StartedAt,
                CompletedAt = toolCall.CompletedAt
            })
            .ToArray();

        if (rows.Length == 0)
        {
            return;
        }

        await _dbContext.AgentToolCalls.AddRangeAsync(rows, cancellationToken);
    }

    public async Task ReplaceCurrentArtifactAsync(AgentArtifact artifact, CancellationToken cancellationToken)
    {
        var threadId = artifact.ThreadId.ToString();
        var currentArtifacts = await _dbContext.AgentArtifacts
            .Where(x => x.ThreadId == threadId && x.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var currentArtifact in currentArtifacts)
        {
            currentArtifact.IsCurrent = false;
        }

        await _dbContext.AgentArtifacts.AddAsync(new AgentArtifactDb
        {
            Id = artifact.Id.ToString(),
            ThreadId = threadId,
            Kind = artifact.Kind,
            PayloadJson = artifact.PayloadJson,
            IsCurrent = true,
            CreatedAt = artifact.CreatedAt
        }, cancellationToken);
    }

    public async Task<AgentProposal?> GetProposalByIdAsync(UlidId proposalId, CancellationToken cancellationToken)
    {
        var proposal = await _dbContext.AgentProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == proposalId.ToString(), cancellationToken);

        return proposal is null ? null : MapProposal(proposal);
    }

    public async Task ReplacePendingProposalAsync(AgentProposal proposal, CancellationToken cancellationToken)
    {
        var threadId = proposal.ThreadId.ToString();
        var existing = await _dbContext.AgentProposals
            .FirstOrDefaultAsync(x => x.Id == proposal.Id.ToString(), cancellationToken);

        if (proposal.State == ProposalState.Pending)
        {
            var currentProposals = await _dbContext.AgentProposals
                .Where(x => x.ThreadId == threadId && x.IsCurrent)
                .ToListAsync(cancellationToken);

            foreach (var currentProposal in currentProposals)
            {
                currentProposal.IsCurrent = false;
            }
        }

        if (existing is null)
        {
            await _dbContext.AgentProposals.AddAsync(new AgentProposalDb
            {
                Id = proposal.Id.ToString(),
                ThreadId = threadId,
                State = (int)proposal.State,
                PayloadJson = proposal.PayloadJson,
                IsCurrent = proposal.State == ProposalState.Pending,
                CreatedAt = proposal.CreatedAt,
                ExpiresAt = proposal.ExpiresAt
            }, cancellationToken);
            return;
        }

        existing.State = (int)proposal.State;
        existing.PayloadJson = proposal.PayloadJson;
        existing.IsCurrent = proposal.State == ProposalState.Pending;
        existing.ExpiresAt = proposal.ExpiresAt;
    }

    private static AgentThread MapThread(AgentThreadDb thread)
    {
        return new AgentThread(
            UlidId.Parse(thread.Id),
            thread.UserId,
            AgentTenantId.Normalize(thread.TenantId),
            thread.BackendId,
            thread.Title,
            thread.CreatedAt,
            thread.UpdatedAt);
    }

    private static AgentProposal MapProposal(AgentProposalDb proposal)
    {
        return new AgentProposal(
            UlidId.Parse(proposal.Id),
            UlidId.Parse(proposal.ThreadId),
            (ProposalState)proposal.State,
            proposal.PayloadJson,
            proposal.CreatedAt,
            proposal.ExpiresAt);
    }
}
