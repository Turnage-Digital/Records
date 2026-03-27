using System.Text.Json.Serialization;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record HistoryEntryDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("actorId")]
    public string? ActorId { get; init; }

    [JsonPropertyName("bag")]
    public object? Bag { get; init; }
}

public sealed record HistoryPageDto
{
    [JsonPropertyName("items")]
    public HistoryEntryDto[] Items { get; init; } = [];

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }
}