using System.Text.Json.Serialization;

namespace Records.Agents.Contracts.Dtos;

public sealed record AgentProviderContextDto
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("pastedText")]
    public string? PastedText { get; init; }

    [JsonPropertyName("currentArtifact")]
    public WorkspaceArtifactDto? CurrentArtifact { get; init; }

    [JsonPropertyName("pendingProposal")]
    public WorkspaceProposalDto? PendingProposal { get; init; }
}

public sealed record AgentTurnResultDto
{
    [JsonPropertyName("assistantMessage")]
    public string AssistantMessage { get; init; } = string.Empty;

    [JsonPropertyName("artifact")]
    public WorkspaceArtifactDto? Artifact { get; init; }

    [JsonPropertyName("proposal")]
    public WorkspaceProposalDto? Proposal { get; init; }
}

public sealed record AgentStreamEventDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("artifact")]
    public WorkspaceArtifactDto? Artifact { get; init; }

    [JsonPropertyName("proposal")]
    public WorkspaceProposalDto? Proposal { get; init; }
}

public sealed record WorkspaceSearchRequestDto
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("currentArtifact")]
    public WorkspaceArtifactDto? CurrentArtifact { get; init; }
}

public sealed record WorkspaceEntityRequestDto
{
    [JsonPropertyName("collectionId")]
    public string CollectionId { get; init; } = string.Empty;

    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;
}

public sealed record WorkspaceResolveSchemaRequestDto
{
    [JsonPropertyName("collectionId")]
    public string CollectionId { get; init; } = string.Empty;
}

public sealed record WorkspaceProposeUpdateRequestDto
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("pastedText")]
    public string? PastedText { get; init; }

    [JsonPropertyName("currentArtifact")]
    public WorkspaceArtifactDto? CurrentArtifact { get; init; }

    [JsonPropertyName("pendingProposal")]
    public WorkspaceProposalDto? PendingProposal { get; init; }
}

public sealed record WorkspaceProposeCreateRequestDto
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("pastedText")]
    public string? PastedText { get; init; }

    [JsonPropertyName("currentArtifact")]
    public WorkspaceArtifactDto? CurrentArtifact { get; init; }
}

public sealed record WorkspaceApplyProposalRequestDto
{
    [JsonPropertyName("proposal")]
    public WorkspaceProposalDto Proposal { get; init; } = new();
}
