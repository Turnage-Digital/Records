using System.Text.Json.Serialization;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetPagedRecordsDto
{
    [JsonPropertyName("id")]
    public UlidId? RecordsetId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; init; }

    [JsonPropertyName("items")]
    public RecordListItemDto[] Items { get; init; } = [];
}

public sealed record RecordListItemDto
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("recordsetId")]
    public UlidId? RecordsetId { get; init; }

    [JsonPropertyName("bag")]
    public object Bag { get; init; } = new { };
}

public sealed record RecordItemDetailsDto
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("recordsetId")]
    public UlidId? RecordsetId { get; init; }

    [JsonPropertyName("bag")]
    public object Bag { get; init; } = new { };
}