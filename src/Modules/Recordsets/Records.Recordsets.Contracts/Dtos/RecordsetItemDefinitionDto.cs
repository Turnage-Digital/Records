using System.Text.Json.Serialization;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetItemDefinitionDto
{
    [JsonPropertyName("id")]
    public UlidId? Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("columns")]
    public RecordsetColumnDto[] Columns { get; init; } = [];

    [JsonPropertyName("statuses")]
    public RecordsetStatusDto[] Statuses { get; init; } = [];

    [JsonPropertyName("transitions")]
    public RecordsetStatusTransitionDto[] Transitions { get; init; } = [];
}

public sealed record RecordsetColumnDto
{
    [JsonPropertyName("key")]
    public string? StorageKey { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("property")]
    public string Property { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public ColumnType Type { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("allowedValues")]
    public string[]? AllowedValues { get; init; }

    [JsonPropertyName("minNumber")]
    public decimal? MinNumber { get; init; }

    [JsonPropertyName("maxNumber")]
    public decimal? MaxNumber { get; init; }

    [JsonPropertyName("regex")]
    public string? Regex { get; init; }
}

public sealed record RecordsetStatusDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;
}

public sealed record RecordsetStatusTransitionDto
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("allowedNext")]
    public string[] AllowedNext { get; init; } = [];
}