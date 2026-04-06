using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Contracts.Projections;
using Records.Agents.Contracts.Queries;
using Records.Agents.Domain;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class AgentConversationService(
    IAgentsUnitOfWork unitOfWork,
    IAgentThreadQueries threadQueries,
    IAgentBackendRegistry backendRegistry,
    IAgentProvider agentProvider,
    IAgentProposalExecutor proposalExecutor,
    IAgentThreadProjectionWriter threadProjectionWriter,
    ICurrentUserAccess currentUserAccess,
    ITenantContext tenantContext,
    IAgentThreadStream threadStream
) : IAgentConversationService
{
    public async Task<AgentThreadSummaryDto> CreateThreadAsync(
        string? title,
        string? backendId,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserAccess.GetCurrentUserIdOrThrow().ToString();
        var now = DateTimeOffset.UtcNow;
        var resolvedBackendId = backendRegistry.GetRequiredBackend(backendId).BackendId;
        var thread = AgentThread.Create(
            UlidId.NewUlid(),
            userId,
            AgentTenantId.Normalize(tenantContext.TenantId),
            resolvedBackendId,
            title,
            now);

        await unitOfWork.AddThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await UpsertThreadProjectionAsync(thread, cancellationToken);

        return new AgentThreadSummaryDto
        {
            Id = thread.Id.ToString(),
            Title = thread.Title,
            UpdatedAt = thread.UpdatedAt
        };
    }

    public async Task<AgentThreadDto?> PostTurnAsync(
        string threadId,
        string message,
        string? pastedText,
        CancellationToken cancellationToken
    )
    {
        var thread = await RequireThreadAsync(threadId, cancellationToken);
        if (thread is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var userTurn = new AgentTurn(
            UlidId.NewUlid(),
            thread.Id,
            AgentTurnRole.User,
            message.Trim(),
            string.IsNullOrWhiteSpace(pastedText) ? null : pastedText.Trim(),
            now);

        await unitOfWork.AddTurnAsync(userTurn, cancellationToken);
        if (thread.Title == "New conversation")
        {
            thread.UpdateTitle(ToTitle(message), now);
        }
        else
        {
            thread.Touch(now);
        }

        await unitOfWork.UpdateThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await UpsertThreadProjectionAsync(thread, cancellationToken);

        var currentState = await threadQueries.GetByIdAsync(threadId, cancellationToken);
        if (currentState is null)
        {
            return null;
        }

        var result = await agentProvider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = threadId,
            BackendId = thread.BackendId,
            Message = message,
            PastedText = pastedText,
            CurrentArtifact = currentState.CurrentArtifact,
            PendingProposal = currentState.PendingProposal
        }, cancellationToken);

        var assistantTurn = new AgentTurn(
            UlidId.NewUlid(),
            thread.Id,
            AgentTurnRole.Assistant,
            result.AssistantMessage,
            null,
            DateTimeOffset.UtcNow);
        await unitOfWork.AddTurnAsync(assistantTurn, cancellationToken);
        await unitOfWork.AddToolCallsAsync(
            result.ToolCalls.Select(toolCall => new AgentToolCall(
                toolCall.Id,
                thread.Id.ToString(),
                assistantTurn.Id.ToString(),
                toolCall.Name,
                toolCall.ArgumentsJson,
                toolCall.Status,
                toolCall.Summary,
                toolCall.Error,
                toolCall.StartedAt,
                toolCall.CompletedAt)),
            cancellationToken);

        if (result.Artifact is not null)
        {
            await unitOfWork.ReplaceCurrentArtifactAsync(new AgentArtifact(
                UlidId.NewUlid(),
                thread.Id,
                result.Artifact.Kind,
                AgentJsonSerializer.Serialize(result.Artifact),
                DateTimeOffset.UtcNow), cancellationToken);
        }

        if (result.Proposal is not null && UlidId.TryParse(result.Proposal.ProposalId, out var proposalId))
        {
            await unitOfWork.ReplacePendingProposalAsync(new AgentProposal(
                proposalId,
                thread.Id,
                ProposalState.Pending,
                AgentJsonSerializer.Serialize(result.Proposal),
                DateTimeOffset.UtcNow,
                result.Proposal.ExpiresAt), cancellationToken);
        }

        thread.Touch(DateTimeOffset.UtcNow);
        await unitOfWork.UpdateThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await UpsertThreadProjectionAsync(thread, cancellationToken);

        foreach (var toolCall in result.ToolCalls)
        {
            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = "tool_call_started",
                OccurredAt = toolCall.StartedAt,
                ToolCall = toolCall
            }, cancellationToken);

            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = toolCall.Status is "failed" or "rejected"
                    ? "tool_call_failed"
                    : "tool_call_completed",
                OccurredAt = toolCall.CompletedAt ?? toolCall.StartedAt,
                ToolCall = toolCall
            }, cancellationToken);
        }

        await threadStream.PublishAsync(threadId, new AgentStreamEventDto
        {
            Type = "assistant_final_message",
            OccurredAt = DateTimeOffset.UtcNow,
            Message = result.AssistantMessage
        }, cancellationToken);

        if (result.Artifact is not null)
        {
            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = "artifact_replace",
                OccurredAt = DateTimeOffset.UtcNow,
                Artifact = result.Artifact
            }, cancellationToken);
        }

        if (result.Proposal is not null)
        {
            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = "proposal_created",
                OccurredAt = DateTimeOffset.UtcNow,
                Proposal = result.Proposal
            }, cancellationToken);
        }

        return await threadQueries.GetByIdAsync(threadId, cancellationToken);
    }

    public async Task<AgentThreadDto?> ConfirmProposalAsync(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    )
    {
        var thread = await RequireThreadAsync(threadId, cancellationToken);
        if (thread is null || !UlidId.TryParse(proposalId, out var proposalUlid))
        {
            return null;
        }

        var proposal = await unitOfWork.GetProposalByIdAsync(proposalUlid, cancellationToken);
        if (proposal is null || proposal.ThreadId != thread.Id)
        {
            return null;
        }

        proposal.ExpireIfNeeded(DateTimeOffset.UtcNow);
        if (proposal.State == ProposalState.Expired)
        {
            await unitOfWork.ReplacePendingProposalAsync(proposal, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = "proposal_expired",
                OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);
            return await threadQueries.GetByIdAsync(threadId, cancellationToken);
        }

        var proposalDto = AgentJsonSerializer.Deserialize<WorkspaceProposalDto>(proposal.PayloadJson);
        if (proposalDto is null)
        {
            return null;
        }

        var updatedEntity = await proposalExecutor.ApplyConfirmedProposalAsync(
            thread.BackendId,
            proposalDto,
            cancellationToken);

        proposal.Confirm();
        await unitOfWork.ReplacePendingProposalAsync(proposal, cancellationToken);

        if (updatedEntity is not null)
        {
            var artifact = new WorkspaceArtifactDto
            {
                Kind = "detail",
                Title = updatedEntity.DisplayName,
                Detail = updatedEntity,
                Editor = updatedEntity.Schema
            };
            await unitOfWork.ReplaceCurrentArtifactAsync(new AgentArtifact(
                UlidId.NewUlid(),
                thread.Id,
                artifact.Kind,
                AgentJsonSerializer.Serialize(artifact),
                DateTimeOffset.UtcNow), cancellationToken);

            await threadStream.PublishAsync(threadId, new AgentStreamEventDto
            {
                Type = "artifact_replace",
                OccurredAt = DateTimeOffset.UtcNow,
                Artifact = artifact
            }, cancellationToken);
        }

        await unitOfWork.AddTurnAsync(new AgentTurn(
            UlidId.NewUlid(),
            thread.Id,
            AgentTurnRole.Assistant,
            "I applied the proposed changes.",
            null,
            DateTimeOffset.UtcNow), cancellationToken);

        thread.Touch(DateTimeOffset.UtcNow);
        await unitOfWork.UpdateThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await UpsertThreadProjectionAsync(thread, cancellationToken);

        await threadStream.PublishAsync(threadId, new AgentStreamEventDto
        {
            Type = "proposal_applied",
            OccurredAt = DateTimeOffset.UtcNow,
            Message = "Proposal applied"
        }, cancellationToken);

        return await threadQueries.GetByIdAsync(threadId, cancellationToken);
    }

    public async Task<AgentThreadDto?> RejectProposalAsync(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    )
    {
        var thread = await RequireThreadAsync(threadId, cancellationToken);
        if (thread is null || !UlidId.TryParse(proposalId, out var proposalUlid))
        {
            return null;
        }

        var proposal = await unitOfWork.GetProposalByIdAsync(proposalUlid, cancellationToken);
        if (proposal is null || proposal.ThreadId != thread.Id)
        {
            return null;
        }

        proposal.Reject();
        await unitOfWork.ReplacePendingProposalAsync(proposal, cancellationToken);
        await unitOfWork.AddTurnAsync(new AgentTurn(
            UlidId.NewUlid(),
            thread.Id,
            AgentTurnRole.Assistant,
            "I discarded that proposal.",
            null,
            DateTimeOffset.UtcNow), cancellationToken);
        thread.Touch(DateTimeOffset.UtcNow);
        await unitOfWork.UpdateThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await UpsertThreadProjectionAsync(thread, cancellationToken);

        await threadStream.PublishAsync(threadId, new AgentStreamEventDto
        {
            Type = "proposal_expired",
            OccurredAt = DateTimeOffset.UtcNow,
            Message = "Proposal rejected"
        }, cancellationToken);

        return await threadQueries.GetByIdAsync(threadId, cancellationToken);
    }

    private Task UpsertThreadProjectionAsync(AgentThread thread, CancellationToken cancellationToken)
    {
        return threadProjectionWriter.UpsertAsync(
            new AgentThreadProjectionModel(
                thread.Id.ToString(),
                thread.UserId,
                thread.TenantId,
                thread.Title,
                thread.UpdatedAt),
            cancellationToken);
    }

    private async Task<AgentThread?> RequireThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(threadId, out var threadUlid))
        {
            return null;
        }

        var thread = await unitOfWork.GetThreadByIdAsync(threadUlid, cancellationToken);
        if (thread is null)
        {
            return null;
        }

        var userId = currentUserAccess.GetCurrentUserIdOrThrow().ToString();
        if (!string.Equals(thread.UserId, userId, StringComparison.Ordinal))
        {
            return null;
        }

        if (AgentTenantId.Normalize(thread.TenantId) != AgentTenantId.Normalize(tenantContext.TenantId))
        {
            return null;
        }

        return thread;
    }

    private static string ToTitle(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length <= 48)
        {
            return trimmed;
        }

        return trimmed[..45].TrimEnd() + "...";
    }
}
