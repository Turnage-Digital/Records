using System.Text.Json.Serialization;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetNameDto
{
    [JsonPropertyName("id")]
    public UlidId? Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
