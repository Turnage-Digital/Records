using System.Text.Json.Serialization;

namespace Records.Agents.Contracts.Dtos;

public sealed record WorkspaceArtifactDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("grid")]
    public WorkspaceGridDto? Grid { get; init; }

    [JsonPropertyName("detail")]
    public WorkspaceEntityDto? Detail { get; init; }

    [JsonPropertyName("editor")]
    public WorkspaceEditorSchemaDto? Editor { get; init; }

    [JsonPropertyName("history")]
    public WorkspaceHistoryDto? History { get; init; }

    [JsonPropertyName("notifications")]
    public WorkspaceNotificationsDto? Notifications { get; init; }
}

public sealed record WorkspaceGridDto
{
    [JsonPropertyName("collectionId")]
    public string CollectionId { get; init; } = string.Empty;

    [JsonPropertyName("collectionLabel")]
    public string CollectionLabel { get; init; } = string.Empty;

    [JsonPropertyName("resolvedFilters")]
    public string[] ResolvedFilters { get; init; } = [];

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public long TotalCount { get; init; }

    [JsonPropertyName("columns")]
    public WorkspaceGridColumnDto[] Columns { get; init; } = [];

    [JsonPropertyName("rows")]
    public WorkspaceGridRowDto[] Rows { get; init; } = [];
}

public sealed record WorkspaceGridColumnDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";
}

public sealed record WorkspaceGridRowDto
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public WorkspaceAttributeDto[] Attributes { get; init; } = [];

    [JsonPropertyName("availableActions")]
    public string[] AvailableActions { get; init; } = [];
}

public sealed record WorkspaceEntityDto
{
    [JsonPropertyName("entityType")]
    public string EntityType { get; init; } = string.Empty;

    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("collectionId")]
    public string CollectionId { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public WorkspaceAttributeDto[] Attributes { get; init; } = [];

    [JsonPropertyName("schema")]
    public WorkspaceEditorSchemaDto? Schema { get; init; }
}

public sealed record WorkspaceAttributeDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("displayValue")]
    public string? DisplayValue { get; init; }
}

public sealed record WorkspaceEditorSchemaDto
{
    [JsonPropertyName("collectionId")]
    public string CollectionId { get; init; } = string.Empty;

    [JsonPropertyName("collectionLabel")]
    public string CollectionLabel { get; init; } = string.Empty;

    [JsonPropertyName("fields")]
    public WorkspaceSchemaFieldDto[] Fields { get; init; } = [];

    [JsonPropertyName("stateTransitions")]
    public WorkspaceStateTransitionDto[] StateTransitions { get; init; } = [];
}

public sealed record WorkspaceSchemaFieldDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("allowedValues")]
    public string[] AllowedValues { get; init; } = [];

    [JsonPropertyName("validationHint")]
    public string? ValidationHint { get; init; }
}

public sealed record WorkspaceStateTransitionDto
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("allowedNext")]
    public string[] AllowedNext { get; init; } = [];
}

public sealed record WorkspaceProposalDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "update";

    [JsonPropertyName("proposalId")]
    public string ProposalId { get; init; } = string.Empty;

    [JsonPropertyName("target")]
    public WorkspaceEntityDto Target { get; init; } = new();

    [JsonPropertyName("current")]
    public WorkspaceEntityDto Current { get; init; } = new();

    [JsonPropertyName("proposed")]
    public WorkspaceEntityDto Proposed { get; init; } = new();

    [JsonPropertyName("diffs")]
    public WorkspaceProposalDiffDto[] Diffs { get; init; } = [];

    [JsonPropertyName("rationale")]
    public string Rationale { get; init; } = string.Empty;

    [JsonPropertyName("sourceExcerpt")]
    public string? SourceExcerpt { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = "pending";

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed record WorkspaceProposalDiffDto
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

public sealed record WorkspaceHistoryDto
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("entries")]
    public WorkspaceHistoryEntryDto[] Entries { get; init; } = [];
}

public sealed record WorkspaceHistoryEntryDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("actorId")]
    public string? ActorId { get; init; }

    [JsonPropertyName("attributes")]
    public WorkspaceAttributeDto[] Attributes { get; init; } = [];
}

public sealed record WorkspaceNotificationsDto
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; init; }

    [JsonPropertyName("items")]
    public WorkspaceNotificationDto[] Items { get; init; } = [];
}

public sealed record WorkspaceNotificationDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }
}
