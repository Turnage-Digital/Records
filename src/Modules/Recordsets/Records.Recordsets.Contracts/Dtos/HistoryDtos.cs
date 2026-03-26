using System.Text.Json.Serialization;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record HistoryEntryDto
{
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;

    [JsonPropertyName("on")] public DateTimeOffset On { get; init; }

    [JsonPropertyName("by")] public string? By { get; init; }

    [JsonPropertyName("bag")] public object? Bag { get; init; }
}

public sealed record HistoryPageDto
{
    [JsonPropertyName("items")] public HistoryEntryDto[] Items { get; init; } = [];

    [JsonPropertyName("page")] public int Page { get; init; }

    [JsonPropertyName("pageSize")] public int PageSize { get; init; }

    [JsonPropertyName("total")] public int Total { get; init; }
}