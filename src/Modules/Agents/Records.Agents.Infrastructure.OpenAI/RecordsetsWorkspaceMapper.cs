using System.Text.Json;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.Sql;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Agents.Infrastructure.OpenAI;

internal static class RecordsetsWorkspaceMapper
{
    public static string SummarizeToolResult(string toolName, string outputJson)
    {
        return toolName switch
        {
            "list_recordsets" => SummarizeRecordsets(outputJson),
            "resolve_record_schema" => SummarizeSchema(outputJson),
            "search_records" => SummarizeSearch(outputJson),
            "get_record" => SummarizeRecord(outputJson),
            "get_record_history" => SummarizeHistory(outputJson),
            "validate_record_create" => SummarizeProposal(outputJson, "create"),
            "validate_record_update" => SummarizeProposal(outputJson, "update"),
            "apply_record_create" => SummarizeRecord(outputJson),
            "apply_record_update" => SummarizeRecord(outputJson),
            _ => "Tool completed."
        };
    }

    public static WorkspaceArtifactDto? TryMapArtifact(string toolName, string outputJson)
    {
        return toolName switch
        {
            "search_records" => Deserialize<RecordsetSearchToolResultDto>(outputJson) is { } search
                ? new WorkspaceArtifactDto
                {
                    Kind = "grid",
                    Title = search.Page.Name,
                    Grid = MapGrid(search)
                }
                : null,
            "get_record" => Deserialize<RecordsetDetailToolResultDto>(outputJson) is { } detail
                ? CreateDetailArtifact(detail)
                : null,
            "get_record_history" => Deserialize<RecordsetHistoryToolResultDto>(outputJson) is { } history
                ? new WorkspaceArtifactDto
                {
                    Kind = "history",
                    Title = "Activity history",
                    History = MapHistory(history)
                }
                : null,
            "apply_record_create" => Deserialize<RecordsetApplyToolResultDto>(outputJson) is { } created
                ? CreateDetailArtifact(created)
                : null,
            "apply_record_update" => Deserialize<RecordsetApplyToolResultDto>(outputJson) is { } updated
                ? CreateDetailArtifact(updated)
                : null,
            _ => null
        };
    }

    public static WorkspaceProposalDto? TryMapProposal(string toolName, string outputJson)
    {
        if (toolName is not ("validate_record_create" or "validate_record_update"))
        {
            return null;
        }

        var proposal = Deserialize<RecordMutationProposalDto>(outputJson);
        if (proposal is null)
        {
            return null;
        }

        var schema = MapSchema(proposal.Schema);
        var current = MapEntity(proposal.Current, schema, proposal.RecordsetName);
        var proposed = MapEntity(proposal.Proposed, schema, proposal.RecordsetName);
        var target = string.Equals(proposal.Kind, "create", StringComparison.OrdinalIgnoreCase)
            ? proposed
            : current;

        return new WorkspaceProposalDto
        {
            Kind = proposal.Kind,
            ProposalId = proposal.ProposalId,
            Target = target,
            Current = current,
            Proposed = proposed,
            Diffs = proposal.Diffs
                .Select(diff => new WorkspaceProposalDiffDto
                {
                    Key = diff.Key,
                    Label = diff.Label,
                    Before = diff.Before,
                    After = diff.After
                })
                .ToArray(),
            Rationale = proposal.Rationale,
            SourceExcerpt = proposal.SourceExcerpt,
            State = "pending",
            ExpiresAt = proposal.ExpiresAt
        };
    }

    public static WorkspaceEntityDto? TryMapAppliedEntity(string outputJson)
    {
        var result = Deserialize<RecordsetApplyToolResultDto>(outputJson);
        return result is null ? null : MapEntity(result.Detail, MapSchema(result.Schema), result.Schema.Name);
    }

    private static WorkspaceArtifactDto CreateDetailArtifact(RecordsetDetailToolResultDto detail)
    {
        var schema = MapSchema(detail.Schema);
        var entity = MapEntity(detail.Detail, schema, detail.Schema.Name);
        return new WorkspaceArtifactDto
        {
            Kind = "detail",
            Title = entity.DisplayName,
            Detail = entity,
            Editor = schema
        };
    }

    private static WorkspaceArtifactDto CreateDetailArtifact(RecordsetApplyToolResultDto result)
    {
        var schema = MapSchema(result.Schema);
        var entity = MapEntity(result.Detail, schema, result.Schema.Name);
        return new WorkspaceArtifactDto
        {
            Kind = "detail",
            Title = entity.DisplayName,
            Detail = entity,
            Editor = schema
        };
    }

    private static WorkspaceGridDto MapGrid(RecordsetSearchToolResultDto search)
    {
        var schema = MapSchema(search.Schema);
        return new WorkspaceGridDto
        {
            CollectionId = search.Page.RecordsetId?.ToString() ?? string.Empty,
            CollectionLabel = search.Page.Name,
            ResolvedFilters = [],
            Page = 0,
            PageSize = search.Page.Items.Length,
            TotalCount = search.Page.Count,
            Columns = schema.Fields
                .Select(field => new WorkspaceGridColumnDto
                {
                    Key = field.Key,
                    Label = field.Label,
                    Type = field.Type
                })
                .ToArray(),
            Rows = search.Page.Items
                .Select(item => MapGridRow(item, schema, search.Page.Name))
                .ToArray()
        };
    }

    private static WorkspaceHistoryDto MapHistory(RecordsetHistoryToolResultDto history)
    {
        return new WorkspaceHistoryDto
        {
            EntityId = history.RecordId.ToString(),
            Entries = history.History.Items
                .Select(entry => new WorkspaceHistoryEntryDto
                {
                    Type = entry.Type,
                    OccurredAt = entry.OccurredAt,
                    ActorId = entry.ActorId,
                    Attributes = ToAttributes(entry.Bag)
                })
                .ToArray()
        };
    }

    private static WorkspaceEditorSchemaDto MapSchema(RecordsetItemDefinitionDto definition)
    {
        return new WorkspaceEditorSchemaDto
        {
            CollectionId = definition.Id?.ToString() ?? string.Empty,
            CollectionLabel = definition.Name,
            Fields = definition.Columns
                .Select(column => new WorkspaceSchemaFieldDto
                {
                    Key = column.Property,
                    Label = column.Name,
                    Type = MapFieldType(column.Type.ToString()),
                    Required = column.Required,
                    AllowedValues = column.AllowedValues ?? [],
                    ValidationHint = column.Regex
                })
                .Append(new WorkspaceSchemaFieldDto
                {
                    Key = "status",
                    Label = "Status",
                    Type = "enum",
                    Required = true,
                    AllowedValues = definition.Statuses.Select(status => status.Name).ToArray()
                })
                .ToArray(),
            StateTransitions = definition.Transitions
                .Select(transition => new WorkspaceStateTransitionDto
                {
                    From = transition.From,
                    AllowedNext = transition.AllowedNext
                })
                .ToArray()
        };
    }

    private static WorkspaceGridRowDto MapGridRow(
        RecordListItemDto item,
        WorkspaceEditorSchemaDto schema,
        string collectionLabel
    )
    {
        var attributes = MapAttributes(item.Bag, schema);
        return new WorkspaceGridRowDto
        {
            EntityId = item.Id?.ToString() ?? string.Empty,
            DisplayName = ResolveDisplayName(attributes, collectionLabel, item.Id?.ToString()),
            Attributes = attributes,
            AvailableActions = ["inspect", "update"]
        };
    }

    private static WorkspaceEntityDto MapEntity(
        RecordItemDetailsDto detail,
        WorkspaceEditorSchemaDto schema,
        string collectionLabel
    )
    {
        var attributes = MapAttributes(detail.Bag, schema);
        return new WorkspaceEntityDto
        {
            EntityType = collectionLabel,
            EntityId = detail.Id?.ToString() ?? string.Empty,
            CollectionId = detail.RecordsetId?.ToString() ?? string.Empty,
            DisplayName = ResolveDisplayName(attributes, collectionLabel, detail.Id?.ToString()),
            Attributes = attributes,
            Schema = schema
        };
    }

    private static WorkspaceAttributeDto[] MapAttributes(object bag, WorkspaceEditorSchemaDto schema)
    {
        var values = ToDictionary(bag);
        return schema.Fields
            .Select(field =>
            {
                values.TryGetValue(field.Key, out var value);
                return new WorkspaceAttributeDto
                {
                    Key = field.Key,
                    Label = field.Label,
                    Type = field.Type,
                    Value = value,
                    DisplayValue = value?.ToString()
                };
            })
            .ToArray();
    }

    private static WorkspaceAttributeDto[] ToAttributes(object? bag)
    {
        return ToDictionary(bag)
            .Select(pair => new WorkspaceAttributeDto
            {
                Key = pair.Key,
                Label = pair.Key,
                Type = InferType(pair.Value),
                Value = pair.Value,
                DisplayValue = pair.Value?.ToString()
            })
            .ToArray();
    }

    private static Dictionary<string, object?> ToDictionary(object? bag)
    {
        if (bag is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (bag is Dictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        var element = bag is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(bag, AgentJsonSerializer.Options), AgentJsonSerializer.Options);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ReadJsonValue(property.Value);
        }

        return result;
    }

    private static object? ReadJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static string ResolveDisplayName(
        IEnumerable<WorkspaceAttributeDto> attributes,
        string collectionLabel,
        string? fallbackId
    )
    {
        var preferred = attributes.FirstOrDefault(attribute =>
            attribute.Key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
            attribute.Key.Equals("title", StringComparison.OrdinalIgnoreCase) ||
            attribute.Key.Equals("orderNumber", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(preferred?.DisplayValue))
        {
            return preferred.DisplayValue!;
        }

        return string.IsNullOrWhiteSpace(fallbackId)
            ? collectionLabel
            : $"{collectionLabel} #{fallbackId}";
    }

    private static string MapFieldType(string fieldType)
    {
        return fieldType.ToLowerInvariant() switch
        {
            "number" => "number",
            "boolean" => "boolean",
            "date" => "date",
            _ => "text"
        };
    }

    private static string InferType(object? value)
    {
        return value switch
        {
            bool => "boolean",
            byte or short or int or long or decimal or double or float => "number",
            DateTimeOffset or DateTime => "date",
            _ => "text"
        };
    }

    private static T? Deserialize<T>(string json)
    {
        if (TryDeserialize(json, out T? value))
        {
            return value;
        }

        if (!TryExtractContentTextPayload(json, out var payloadJson))
        {
            return default;
        }

        return TryDeserialize(payloadJson, out value)
            ? value
            : default;
    }

    private static bool TryDeserialize<T>(string json, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json, AgentJsonSerializer.Options);
            return value is not null;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }

    private static bool TryExtractContentTextPayload(string json, out string payloadJson)
    {
        payloadJson = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                payloadJson = root.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(payloadJson);
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("text", out var textProperty) ||
                    textProperty.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                payloadJson = textProperty.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(payloadJson))
                {
                    return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string SummarizeRecordsets(string outputJson)
    {
        var recordsets = Deserialize<RecordsetNameDto[]>(outputJson);
        return recordsets is null ? "Listed recordsets." : $"Found {recordsets.Length} recordset(s).";
    }

    private static string SummarizeSchema(string outputJson)
    {
        var schema = Deserialize<RecordsetItemDefinitionDto>(outputJson);
        return schema is null ? "Resolved record schema." : $"Resolved the {schema.Name} schema.";
    }

    private static string SummarizeSearch(string outputJson)
    {
        var result = Deserialize<RecordsetSearchToolResultDto>(outputJson);
        return result is null
            ? "Searched records."
            : $"Found {result.Page.Count} result(s) in {result.Page.Name}.";
    }

    private static string SummarizeRecord(string outputJson)
    {
        var result = Deserialize<RecordsetDetailToolResultDto>(outputJson);
        if (result is not null)
        {
            return $"Loaded a {result.Schema.Name} record.";
        }

        var applied = Deserialize<RecordsetApplyToolResultDto>(outputJson);
        return applied is null ? "Loaded record details." : $"Loaded a {applied.Schema.Name} record.";
    }

    private static string SummarizeHistory(string outputJson)
    {
        var result = Deserialize<RecordsetHistoryToolResultDto>(outputJson);
        return result is null
            ? "Loaded record history."
            : $"Loaded {result.History.Items.Length} history entrie(s).";
    }

    private static string SummarizeProposal(string outputJson, string kind)
    {
        var proposal = Deserialize<RecordMutationProposalDto>(outputJson);
        return proposal is null
            ? $"Prepared a {kind} proposal."
            : $"Prepared a {kind} proposal with {proposal.Diffs.Length} change(s).";
    }
}
