using System.Text.RegularExpressions;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Infrastructure.Sql;

public sealed partial class RuleBasedAgentProvider : IAgentProvider
{
    public async Task<AgentTurnResultDto> ExecuteTurnAsync(
        AgentProviderContextDto context,
        IWorkspaceBackendAdapter backendAdapter,
        CancellationToken cancellationToken
    )
    {
        if (LooksLikeHistoryRequest(context.Message))
        {
            var entityRequest = ResolveEntityRequest(context.Message, context.CurrentArtifact);
            if (entityRequest is not null)
            {
                var history = await backendAdapter.GetHistoryAsync(entityRequest, cancellationToken);
                if (history is not null)
                {
                    return new AgentTurnResultDto
                    {
                        AssistantMessage = "Here is the activity history for that record.",
                        Artifact = new WorkspaceArtifactDto
                        {
                            Kind = "history",
                            Title = "Activity history",
                            History = history
                        }
                    };
                }
            }
        }

        if (LooksLikeCreateRequest(context.Message))
        {
            var proposal = await backendAdapter.ProposeCreateAsync(new WorkspaceProposeCreateRequestDto
            {
                Prompt = context.Message,
                PastedText = context.PastedText,
                CurrentArtifact = context.CurrentArtifact
            }, cancellationToken);

            if (proposal is not null)
            {
                return new AgentTurnResultDto
                {
                    AssistantMessage = $"I prepared a draft {proposal.Target.EntityType.ToLowerInvariant()} with {proposal.Diffs.Length} field value(s). Review it before creating.",
                    Proposal = proposal
                };
            }

            return new AgentTurnResultDto
            {
                AssistantMessage = "I could not build a record draft from that request yet. Try naming the recordset and the fields you want to set."
            };
        }

        if (LooksLikeUpdateRequest(context.Message) || !string.IsNullOrWhiteSpace(context.PastedText))
        {
            var proposal = await backendAdapter.ProposeUpdateAsync(new WorkspaceProposeUpdateRequestDto
            {
                Prompt = context.Message,
                PastedText = context.PastedText,
                CurrentArtifact = context.CurrentArtifact,
                PendingProposal = context.PendingProposal
            }, cancellationToken);

            if (proposal is not null)
            {
                return new AgentTurnResultDto
                {
                    AssistantMessage = $"I prepared a proposal with {proposal.Diffs.Length} change(s). Review it before applying.",
                    Proposal = proposal
                };
            }

            return new AgentTurnResultDto
            {
                AssistantMessage = "I could not build an update proposal from that request yet."
            };
        }

        if (LooksLikeDetailRequest(context.Message))
        {
            var entityRequest = ResolveEntityRequest(context.Message, context.CurrentArtifact);
            if (entityRequest is not null)
            {
                var detail = await backendAdapter.GetEntityAsync(entityRequest, cancellationToken);
                if (detail is not null)
                {
                    return new AgentTurnResultDto
                    {
                        AssistantMessage = $"Here are the details for {detail.DisplayName}.",
                        Artifact = new WorkspaceArtifactDto
                        {
                            Kind = "detail",
                            Title = detail.DisplayName,
                            Detail = detail,
                            Editor = detail.Schema
                        }
                    };
                }
            }
        }

        var grid = await backendAdapter.SearchAsync(new WorkspaceSearchRequestDto
        {
            Query = context.Message,
            CurrentArtifact = context.CurrentArtifact
        }, cancellationToken);

        if (grid is not null)
        {
            return new AgentTurnResultDto
            {
                AssistantMessage = $"I found {grid.TotalCount} result(s) in {grid.CollectionLabel}.",
                Artifact = new WorkspaceArtifactDto
                {
                    Kind = "grid",
                    Title = grid.CollectionLabel,
                    Grid = grid
                }
            };
        }

        return new AgentTurnResultDto
        {
            AssistantMessage = "I could not resolve that request yet. Try naming the thing you want to find or the record you want to update."
        };
    }

    private static bool LooksLikeCreateRequest(string message)
    {
        var trimmed = message.TrimStart();
        return trimmed.StartsWith("create ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("add ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("new ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("log ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUpdateRequest(string message)
    {
        return message.Contains("update", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("change", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("set ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeHistoryRequest(string message)
    {
        return message.Contains("history", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeDetailRequest(string message)
    {
        return message.Contains("inspect", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("details", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("open ", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("show ", StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceEntityRequestDto? ResolveEntityRequest(
        string message,
        WorkspaceArtifactDto? currentArtifact
    )
    {
        var explicitId = ExtractEntityId(message);
        var collectionId = currentArtifact?.Detail?.CollectionId ?? currentArtifact?.Grid?.CollectionId;

        if (!string.IsNullOrWhiteSpace(collectionId) && !string.IsNullOrWhiteSpace(explicitId))
        {
            return new WorkspaceEntityRequestDto
            {
                CollectionId = collectionId,
                EntityId = explicitId
            };
        }

        if (!string.IsNullOrWhiteSpace(collectionId) &&
            currentArtifact?.Grid?.Rows.Length == 1)
        {
            return new WorkspaceEntityRequestDto
            {
                CollectionId = collectionId,
                EntityId = currentArtifact.Grid.Rows[0].EntityId
            };
        }

        if (currentArtifact?.Detail is not null)
        {
            return new WorkspaceEntityRequestDto
            {
                CollectionId = currentArtifact.Detail.CollectionId,
                EntityId = currentArtifact.Detail.EntityId
            };
        }

        return null;
    }

    private static string? ExtractEntityId(string message)
    {
        var match = EntityIdRegex().Match(message);
        return match.Success ? match.Groups["id"].Value : null;
    }

    [GeneratedRegex(@"\b(?:record|order)\s+(?<id>\d+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EntityIdRegex();
}
