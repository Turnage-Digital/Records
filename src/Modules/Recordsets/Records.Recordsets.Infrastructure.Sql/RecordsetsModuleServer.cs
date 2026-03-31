using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Recordsets.Application.Commands;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetsModuleServer(
    IRecordsetQueries recordsetQueries,
    IRecordQueries recordQueries,
    IRecordsetsUnitOfWork unitOfWork,
    IRecordBagValidator bagValidator,
    IMediator mediator
) : IAgentModuleServer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string ModuleId => "recordsets";

    public IReadOnlyList<AgentModuleToolDescriptor> Tools =>
    [
        new("list_recordsets", "List recordsets for discovery."),
        new("resolve_record_schema", "Resolve a generic record editor schema."),
        new("search_records", "Search records using structured filters."),
        new("get_record", "Get a single record as a workspace entity."),
        new("get_record_history", "Get a record history feed."),
        new("validate_record_create", "Prepare a structured create proposal."),
        new("apply_record_create", "Apply a confirmed record create."),
        new("validate_record_update", "Prepare a structured update proposal."),
        new("apply_record_update", "Apply a confirmed record update.")
    ];

    public async Task<JsonNode?> InvokeAsync(
        string operation,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        return operation switch
        {
            "list_recordsets" => JsonSerializer.SerializeToNode(await ListRecordsetsAsync(cancellationToken)),
            "resolve_record_schema" => JsonSerializer.SerializeToNode(
                await ResolveSchemaAsync(arguments, cancellationToken),
                SerializerOptions),
            "search_records" => JsonSerializer.SerializeToNode(
                await SearchRecordsAsync(arguments, cancellationToken),
                SerializerOptions),
            "get_record" => JsonSerializer.SerializeToNode(
                await GetRecordAsync(arguments, cancellationToken),
                SerializerOptions),
            "get_record_history" => JsonSerializer.SerializeToNode(
                await GetRecordHistoryAsync(arguments, cancellationToken),
                SerializerOptions),
            "validate_record_create" => JsonSerializer.SerializeToNode(
                await ValidateRecordCreateAsync(arguments, cancellationToken),
                SerializerOptions),
            "apply_record_create" => JsonSerializer.SerializeToNode(
                await ApplyRecordCreateAsync(arguments, cancellationToken),
                SerializerOptions),
            "validate_record_update" => JsonSerializer.SerializeToNode(
                await ValidateRecordUpdateAsync(arguments, cancellationToken),
                SerializerOptions),
            "apply_record_update" => JsonSerializer.SerializeToNode(
                await ApplyRecordUpdateAsync(arguments, cancellationToken),
                SerializerOptions),
            _ => null
        };
    }

    private async Task<IReadOnlyList<object>> ListRecordsetsAsync(CancellationToken cancellationToken)
    {
        var recordsets = await recordsetQueries.ListNamesAsync(cancellationToken);
        return recordsets
            .Select(recordset => (object)new
            {
                id = recordset.Id?.ToString() ?? string.Empty,
                name = recordset.Name
            })
            .ToArray();
    }

    private async Task<WorkspaceEditorSchemaDto?> ResolveSchemaAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        return definition is null ? null : MapSchema(definition);
    }

    private async Task<WorkspaceGridDto?> SearchRecordsAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var page = ReadInt(arguments, "page", 0);
        var pageSize = ReadInt(arguments, "pageSize", 20);
        var filters = ReadFilters(arguments, "filters");

        var pageResult = await recordQueries.SearchAsync(recordsetId, page, Math.Min(pageSize, 50), filters, cancellationToken);
        if (pageResult is null)
        {
            return null;
        }

        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var schema = definition is null ? null : MapSchema(definition);

        return new WorkspaceGridDto
        {
            CollectionId = recordsetId.ToString(),
            CollectionLabel = pageResult.Name,
            ResolvedFilters = filters.Select(filter => $"{filter.Field} {filter.Operator} {filter.Value}").ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = pageResult.Count,
            Columns = schema?.Fields
                .Select(field => new WorkspaceGridColumnDto
                {
                    Key = field.Key,
                    Label = field.Label,
                    Type = field.Type
                })
                .ToArray() ?? [],
            Rows = pageResult.Items
                .Select(item => MapGridRow(item, schema, pageResult.Name))
                .ToArray()
        };
    }

    private async Task<WorkspaceEntityDto?> GetRecordAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var recordId = ReadInt(arguments, "entityId", -1);
        if (recordId <= 0)
        {
            return null;
        }

        var detail = await recordQueries.GetDetailsAsync(recordsetId, recordId, cancellationToken);
        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (detail is null || definition is null)
        {
            return null;
        }

        return MapEntity(detail, MapSchema(definition), definition.Name);
    }

    private async Task<WorkspaceHistoryDto?> GetRecordHistoryAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var recordId = ReadInt(arguments, "entityId", -1);
        if (recordId <= 0)
        {
            return null;
        }

        var history = await recordQueries.GetRecordHistoryAsync(recordsetId, recordId, 0, 20, cancellationToken);
        if (history is null)
        {
            return null;
        }

        return new WorkspaceHistoryDto
        {
            EntityId = recordId.ToString(),
            Entries = history.Items
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

    private async Task<WorkspaceProposalDto?> ValidateRecordCreateAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var changes = ReadChanges(arguments, "changes");
        if (changes.Count == 0)
        {
            return null;
        }

        var recordset = await unitOfWork.GetRecordsetByIdAsync(recordsetId, cancellationToken);
        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (recordset is null || definition is null)
        {
            return null;
        }

        var schema = MapSchema(definition);
        var currentBag = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var proposedBag = new Dictionary<string, object?>(currentBag, StringComparer.OrdinalIgnoreCase);
        foreach (var change in changes)
        {
            proposedBag[change.Key] = change.Value;
        }

        try
        {
            bagValidator.Validate(recordset, proposedBag);
        }
        catch
        {
            return null;
        }

        var currentEntity = MapEntity(new RecordItemDetailsDto
        {
            RecordsetId = recordsetId,
            Bag = currentBag
        }, schema, definition.Name);
        var proposedEntity = MapEntity(new RecordItemDetailsDto
        {
            RecordsetId = recordsetId,
            Bag = proposedBag
        }, schema, definition.Name);

        return new WorkspaceProposalDto
        {
            Kind = "create",
            ProposalId = UlidId.NewUlid().ToString(),
            Target = proposedEntity,
            Current = currentEntity,
            Proposed = proposedEntity,
            Diffs = BuildDiffs(currentBag, proposedBag, schema),
            Rationale = "Prepared from the requested field values.",
            SourceExcerpt = ReadString(arguments, "sourceExcerpt"),
            State = "pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    private async Task<WorkspaceProposalDto?> ValidateRecordUpdateAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var recordId = ReadInt(arguments, "entityId", -1);
        if (recordId <= 0)
        {
            return null;
        }

        var changes = ReadChanges(arguments, "changes");
        if (changes.Count == 0)
        {
            return null;
        }

        var recordset = await unitOfWork.GetRecordsetByIdAsync(recordsetId, cancellationToken);
        var record = await unitOfWork.GetRecordByIdAsync(recordsetId, recordId, cancellationToken);
        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (recordset is null || record is null || definition is null)
        {
            return null;
        }

        var currentBag = ToDictionary(record.Bag);
        var proposedBag = new Dictionary<string, object?>(currentBag, StringComparer.OrdinalIgnoreCase);
        foreach (var change in changes)
        {
            proposedBag[change.Key] = change.Value;
        }

        bagValidator.Validate(recordset, proposedBag);
        bagValidator.ValidateTransition(recordset, currentBag, proposedBag);

        var schema = MapSchema(definition);
        var currentEntity = MapEntity(new RecordItemDetailsDto
        {
            Id = record.Id,
            RecordsetId = record.RecordsetId,
            Bag = currentBag
        }, schema, definition.Name);
        var proposedEntity = MapEntity(new RecordItemDetailsDto
        {
            Id = record.Id,
            RecordsetId = record.RecordsetId,
            Bag = proposedBag
        }, schema, definition.Name);

        return new WorkspaceProposalDto
        {
            Kind = "update",
            ProposalId = UlidId.NewUlid().ToString(),
            Target = currentEntity,
            Current = currentEntity,
            Proposed = proposedEntity,
            Diffs = BuildDiffs(currentBag, proposedBag, schema),
            Rationale = "Prepared from the requested field changes.",
            SourceExcerpt = ReadString(arguments, "sourceExcerpt"),
            State = "pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    private async Task<WorkspaceEntityDto?> ApplyRecordCreateAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var changes = ReadChanges(arguments, "changes");
        if (changes.Count == 0)
        {
            return null;
        }

        var result = await mediator.Send(
            new CreateRecordCommand(recordsetId, changes),
            cancellationToken);

        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var detail = await recordQueries.GetDetailsAsync(recordsetId, result.RecordId, cancellationToken);
        if (definition is null || detail is null)
        {
            return null;
        }

        return MapEntity(detail, MapSchema(definition), definition.Name);
    }

    private async Task<WorkspaceEntityDto?> ApplyRecordUpdateAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetRecordsetId(arguments, out var recordsetId))
        {
            return null;
        }

        var recordId = ReadInt(arguments, "entityId", -1);
        if (recordId <= 0)
        {
            return null;
        }

        var changes = ReadChanges(arguments, "changes");
        if (changes.Count == 0)
        {
            return null;
        }

        var record = await unitOfWork.GetRecordByIdAsync(recordsetId, recordId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var nextBag = ToDictionary(record.Bag);
        foreach (var change in changes)
        {
            nextBag[change.Key] = change.Value;
        }

        await mediator.Send(
            new UpdateRecordCommand(recordsetId, recordId, nextBag),
            cancellationToken);

        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var detail = await recordQueries.GetDetailsAsync(recordsetId, recordId, cancellationToken);
        if (definition is null || detail is null)
        {
            return null;
        }

        return MapEntity(detail, MapSchema(definition), definition.Name);
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
        WorkspaceEditorSchemaDto? schema,
        string collectionLabel
    )
    {
        var attributes = schema is null ? ToAttributes(item.Bag) : MapAttributes(item.Bag, schema);
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

    private static WorkspaceProposalDiffDto[] BuildDiffs(
        IReadOnlyDictionary<string, object?> currentBag,
        IReadOnlyDictionary<string, object?> proposedBag,
        WorkspaceEditorSchemaDto schema
    )
    {
        return schema.Fields
            .Select(field =>
            {
                currentBag.TryGetValue(field.Key, out var before);
                proposedBag.TryGetValue(field.Key, out var after);
                return new { field.Key, field.Label, Before = before, After = after };
            })
            .Where(diff => !Equals(diff.Before?.ToString(), diff.After?.ToString()))
            .Select(diff => new WorkspaceProposalDiffDto
            {
                Key = diff.Key,
                Label = diff.Label,
                Before = diff.Before,
                After = diff.After
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
            : JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(bag));

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

    private static bool TryGetRecordsetId(IReadOnlyDictionary<string, object?> arguments, out UlidId recordsetId)
    {
        var raw = ReadString(arguments, "collectionId");
        if (!string.IsNullOrWhiteSpace(raw) && UlidId.TryParse(raw, out recordsetId))
        {
            return true;
        }

        recordsetId = default;
        return false;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> arguments, string key, int defaultValue)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var parsed) => parsed,
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => value.ToString()
        };
    }

    private static List<RecordSearchFilterClause> ReadFilters(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is List<Dictionary<string, object?>> dictionaryList)
        {
            return dictionaryList
                .Select(item => new RecordSearchFilterClause
                {
                    Field = item.TryGetValue("field", out var field) ? field?.ToString() ?? string.Empty : string.Empty,
                    Operator = item.TryGetValue("operator", out var op) ? op?.ToString() ?? "equals" : "equals",
                    Value = item.TryGetValue("value", out var filterValue) ? filterValue?.ToString() ?? string.Empty : string.Empty
                })
                .ToList();
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(item => new RecordSearchFilterClause
                {
                    Field = item.TryGetProperty("field", out var field) ? field.GetString() ?? string.Empty : string.Empty,
                    Operator = item.TryGetProperty("operator", out var op) ? op.GetString() ?? "equals" : "equals",
                    Value = item.TryGetProperty("value", out var filterValue) ? filterValue.GetString() ?? string.Empty : string.Empty
                })
                .ToList();
        }

        return [];
    }

    private static Dictionary<string, object?> ReadChanges(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (value is Dictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ReadJsonValue(property.Value);
            }

            return result;
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
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
}
