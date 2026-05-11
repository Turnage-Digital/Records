using System.Text.Json.Serialization;

namespace Records.Agents.Contracts.Dtos;

public sealed record AgentThreadSummaryDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record AgentThreadDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("turns")]
    public AgentTurnDto[] Turns { get; init; } = [];

    [JsonPropertyName("currentArtifact")]
    public WorkspaceArtifactDto? CurrentArtifact { get; init; }

    [JsonPropertyName("pendingProposal")]
    public WorkspaceProposalDto? PendingProposal { get; init; }
}

public sealed record AgentTurnDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("pastedText")]
    public string? PastedText { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("toolCalls")]
    public AgentToolCallDto[] ToolCalls { get; init; } = [];
}
