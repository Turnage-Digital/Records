using System.Text.Json.Serialization;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetSearchToolResultDto
{
    [JsonPropertyName("schema")]
    public RecordsetItemDefinitionDto Schema { get; init; } = new();

    [JsonPropertyName("page")]
    public RecordsetPagedRecordsDto Page { get; init; } = new();
}

public sealed record RecordsetDetailToolResultDto
{
    [JsonPropertyName("schema")]
    public RecordsetItemDefinitionDto Schema { get; init; } = new();

    [JsonPropertyName("detail")]
    public RecordItemDetailsDto Detail { get; init; } = new();
}

public sealed record RecordsetHistoryToolResultDto
{
    [JsonPropertyName("recordsetId")]
    public UlidId? RecordsetId { get; init; }

    [JsonPropertyName("recordId")]
    public int RecordId { get; init; }

    [JsonPropertyName("history")]
    public HistoryPageDto History { get; init; } = new();
}

public sealed record RecordMutationProposalDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "update";

    [JsonPropertyName("proposalId")]
    public string ProposalId { get; init; } = string.Empty;

    [JsonPropertyName("recordsetId")]
    public UlidId? RecordsetId { get; init; }

    [JsonPropertyName("recordsetName")]
    public string RecordsetName { get; init; } = string.Empty;

    [JsonPropertyName("schema")]
    public RecordsetItemDefinitionDto Schema { get; init; } = new();

    [JsonPropertyName("current")]
    public RecordItemDetailsDto Current { get; init; } = new();

    [JsonPropertyName("proposed")]
    public RecordItemDetailsDto Proposed { get; init; } = new();

    [JsonPropertyName("diffs")]
    public RecordMutationDiffDto[] Diffs { get; init; } = [];

    [JsonPropertyName("rationale")]
    public string Rationale { get; init; } = string.Empty;

    [JsonPropertyName("sourceExcerpt")]
    public string? SourceExcerpt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed record RecordMutationDiffDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("before")]
    public object? Before { get; init; }

    [JsonPropertyName("after")]
    public object? After { get; init; }
}

public sealed record RecordsetApplyToolResultDto
{
    [JsonPropertyName("schema")]
    public RecordsetItemDefinitionDto Schema { get; init; } = new();

    [JsonPropertyName("detail")]
    public RecordItemDetailsDto Detail { get; init; } = new();
}
