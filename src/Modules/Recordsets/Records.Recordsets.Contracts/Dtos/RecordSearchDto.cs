using System.Text.Json.Serialization;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordSearchFilterClause
{
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; init; } = "equals";

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}
