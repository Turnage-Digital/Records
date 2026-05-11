using System.Text.Json;
using System.Text.Json.Nodes;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.Sql;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Agents.Infrastructure.OpenAI;

internal static class RecordsetsWorkspaceMapper
{
    public static string CreateToolReceiptJson(
        string toolName,
        string status,
        string? summary,
        string? error,
        WorkspaceArtifactDto? artifact,
        WorkspaceProposalDto? proposal)
    {
        var receipt = new JsonObject
        {
            ["toolName"] = toolName,
            ["status"] = status
        };

        if (!string.IsNullOrWhiteSpace(summary))
        {
            receipt["summary"] = summary;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            receipt["error"] = error;
        }

        if (artifact is not null)
        {
            receipt["artifact"] = CreateArtifactReceiptNode(artifact);
        }

        if (proposal is not null)
        {
            receipt["proposal"] = CreateProposalReceiptNode(proposal);
        }

        return receipt.ToJsonString(AgentJsonSerializer.Options);
    }

    public static string CreateArtifactPromptJson(WorkspaceArtifactDto artifact)
    {
        return artifact.Kind switch
        {
            "grid" when artifact.Grid is not null => new JsonObject
            {
                ["kind"] = artifact.Kind,
                ["title"] = artifact.Title,
                ["grid"] = CreateGridReceiptNode(artifact.Grid)
            }.ToJsonString(AgentJsonSerializer.Options),
            "history" when artifact.History is not null => new JsonObject
            {
                ["kind"] = artifact.Kind,
                ["title"] = artifact.Title,
                ["history"] = CreateHistoryReceiptNode(artifact.History)
            }.ToJsonString(AgentJsonSerializer.Options),
            _ => AgentJsonSerializer.Serialize(artifact)
        };
    }

    public static string CreateProposalPromptJson(WorkspaceProposalDto proposal)
    {
        return AgentJsonSerializer.Serialize(proposal);
    }

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
            "list_recordsets" => TryMapRecordsetsArtifact(outputJson) ??
                                 (Deserialize<RecordsetNameDto[]>(outputJson) is { } recordsets
                                     ? new WorkspaceArtifactDto
                                     {
                                         Kind = "grid",
                                         Title = "Recordsets",
                                         Grid = MapRecordsets(recordsets)
                                     }
                                     : null),
            "resolve_record_schema" => TryMapSchemaArtifact(outputJson) ??
                                       (Deserialize<RecordsetItemDefinitionDto>(outputJson) is { } schema
                                           ? new WorkspaceArtifactDto
                                           {
                                               Kind = "editor",
                                               Title = $"{schema.Name} schema",
                                               Editor = MapSchema(schema)
                                           }
                                           : null),
            "search_records" => TryMapSearchArtifact(outputJson) ??
                                (Deserialize<RecordsetSearchToolResultDto>(outputJson) is { } search
                                    ? new WorkspaceArtifactDto
                                    {
                                        Kind = "grid",
                                        Title = search.Page.Name,
                                        Grid = MapGrid(search)
                                    }
                                    : null),
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

    private static JsonObject CreateArtifactReceiptNode(WorkspaceArtifactDto artifact)
    {
        var node = new JsonObject
        {
            ["kind"] = artifact.Kind
        };

        if (!string.IsNullOrWhiteSpace(artifact.Title))
        {
            node["title"] = artifact.Title;
        }

        switch (artifact.Kind)
        {
            case "grid" when artifact.Grid is not null:
                Merge(node, CreateGridReceiptNode(artifact.Grid));
                break;
            case "detail" when artifact.Detail is not null:
                Merge(node, CreateDetailReceiptNode(artifact.Detail));
                break;
            case "editor" when artifact.Editor is not null:
                Merge(node, CreateEditorReceiptNode(artifact.Editor));
                break;
            case "history" when artifact.History is not null:
                Merge(node, CreateHistoryReceiptNode(artifact.History));
                break;
        }

        return node;
    }

    private static JsonObject CreateGridReceiptNode(WorkspaceGridDto grid)
    {
        return new JsonObject
        {
            ["artifactKind"] = "grid",
            ["collectionId"] = grid.CollectionId,
            ["collectionLabel"] = grid.CollectionLabel,
            ["page"] = grid.Page,
            ["pageSize"] = grid.PageSize,
            ["rowCount"] = grid.Rows.Length,
            ["totalCount"] = grid.TotalCount,
            ["resolvedFilters"] = JsonSerializer.SerializeToNode(grid.ResolvedFilters, AgentJsonSerializer.Options),
            ["columns"] = JsonSerializer.SerializeToNode(
                grid.Columns.Select(column => new
                {
                    column.Key,
                    column.Label,
                    column.Type
                }).ToArray(),
                AgentJsonSerializer.Options),
            ["rowReferences"] = JsonSerializer.SerializeToNode(
                grid.Rows.Select((row, index) => new
                {
                    index = index + 1,
                    row.EntityId,
                    row.DisplayName
                }).ToArray(),
                AgentJsonSerializer.Options)
        };
    }

    private static JsonObject CreateDetailReceiptNode(WorkspaceEntityDto entity)
    {
        return new JsonObject
        {
            ["artifactKind"] = "detail",
            ["collectionId"] = entity.CollectionId,
            ["entityId"] = entity.EntityId,
            ["displayName"] = entity.DisplayName,
            ["entityType"] = entity.EntityType,
            ["attributes"] = JsonSerializer.SerializeToNode(
                entity.Attributes.Select(attribute => new
                {
                    attribute.Key,
                    attribute.Label,
                    attribute.Type,
                    attribute.Value,
                    attribute.DisplayValue
                }).ToArray(),
                AgentJsonSerializer.Options)
        };
    }

    private static JsonObject CreateEditorReceiptNode(WorkspaceEditorSchemaDto editor)
    {
        return new JsonObject
        {
            ["artifactKind"] = "editor",
            ["collectionId"] = editor.CollectionId,
            ["collectionLabel"] = editor.CollectionLabel,
            ["fieldCount"] = editor.Fields.Length,
            ["fields"] = JsonSerializer.SerializeToNode(
                editor.Fields.Select(field => new
                {
                    field.Key,
                    field.Label,
                    field.Type,
                    field.Required,
                    field.AllowedValues,
                    field.ValidationHint
                }).ToArray(),
                AgentJsonSerializer.Options),
            ["stateTransitions"] = JsonSerializer.SerializeToNode(editor.StateTransitions, AgentJsonSerializer.Options)
        };
    }

    private static JsonObject CreateHistoryReceiptNode(WorkspaceHistoryDto history)
    {
        return new JsonObject
        {
            ["artifactKind"] = "history",
            ["entityId"] = history.EntityId,
            ["entryCount"] = history.Entries.Length,
            ["entries"] = JsonSerializer.SerializeToNode(
                history.Entries.Select((entry, index) => new
                {
                    index = index + 1,
                    entry.Type,
                    entry.OccurredAt,
                    entry.ActorId
                }).ToArray(),
                AgentJsonSerializer.Options)
        };
    }

    private static JsonObject CreateProposalReceiptNode(WorkspaceProposalDto proposal)
    {
        return new JsonObject
        {
            ["proposalId"] = proposal.ProposalId,
            ["proposalKind"] = proposal.Kind,
            ["diffCount"] = proposal.Diffs.Length,
            ["target"] = JsonSerializer.SerializeToNode(new
            {
                proposal.Target.CollectionId,
                proposal.Target.EntityId,
                proposal.Target.DisplayName
            }, AgentJsonSerializer.Options),
            ["diffs"] = JsonSerializer.SerializeToNode(
                proposal.Diffs.Select(diff => new
                {
                    diff.Key,
                    diff.Label,
                    diff.Before,
                    diff.After
                }).ToArray(),
                AgentJsonSerializer.Options)
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

    private static WorkspaceArtifactDto? TryMapRecordsetsArtifact(string outputJson)
    {
        using var document = ParsePayloadDocument(outputJson);
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var rows = document.RootElement
            .EnumerateArray()
            .Where(element => element.ValueKind == JsonValueKind.Object)
            .Select(element => new WorkspaceGridRowDto
            {
                EntityId = ReadStringProperty(element, "id") ?? string.Empty,
                DisplayName = ReadStringProperty(element, "name") ?? "Recordset",
                Attributes =
                [
                    new WorkspaceAttributeDto
                    {
                        Key = "name",
                        Label = "Name",
                        Type = "text",
                        Value = ReadStringProperty(element, "name"),
                        DisplayValue = ReadStringProperty(element, "name")
                    },
                    new WorkspaceAttributeDto
                    {
                        Key = "id",
                        Label = "Id",
                        Type = "text",
                        Value = ReadStringProperty(element, "id"),
                        DisplayValue = ReadStringProperty(element, "id")
                    },
                    new WorkspaceAttributeDto
                    {
                        Key = "count",
                        Label = "Count",
                        Type = "number",
                        Value = ReadLongProperty(element, "count"),
                        DisplayValue = ReadLongProperty(element, "count")?.ToString()
                    }
                ],
                AvailableActions = []
            })
            .ToArray();

        return new WorkspaceArtifactDto
        {
            Kind = "grid",
            Title = "Recordsets",
            Grid = new WorkspaceGridDto
            {
                CollectionId = "recordsets",
                CollectionLabel = "Recordsets",
                ResolvedFilters = [],
                Page = 0,
                PageSize = rows.Length,
                TotalCount = rows.Length,
                Columns =
                [
                    new WorkspaceGridColumnDto
                    {
                        Key = "name",
                        Label = "Name",
                        Type = "text"
                    },
                    new WorkspaceGridColumnDto
                    {
                        Key = "id",
                        Label = "Id",
                        Type = "text"
                    },
                    new WorkspaceGridColumnDto
                    {
                        Key = "count",
                        Label = "Count",
                        Type = "number"
                    }
                ],
                Rows = rows
            }
        };
    }

    private static WorkspaceArtifactDto? TryMapSchemaArtifact(string outputJson)
    {
        using var document = ParsePayloadDocument(outputJson);
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var schema = MapSchema(document.RootElement);
        if (string.IsNullOrWhiteSpace(schema.CollectionLabel))
        {
            return null;
        }

        return new WorkspaceArtifactDto
        {
            Kind = "editor",
            Title = $"{schema.CollectionLabel} schema",
            Editor = schema
        };
    }

    private static WorkspaceArtifactDto? TryMapSearchArtifact(string outputJson)
    {
        using var document = ParsePayloadDocument(outputJson);
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!document.RootElement.TryGetProperty("schema", out var schemaElement) ||
            !document.RootElement.TryGetProperty("page", out var pageElement) ||
            schemaElement.ValueKind != JsonValueKind.Object ||
            pageElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var schema = MapSchema(schemaElement);
        var collectionLabel = ReadStringProperty(pageElement, "name") ?? schema.CollectionLabel;
        var collectionId =
            ReadStringProperty(pageElement, "recordsetId") ??
            ReadStringProperty(pageElement, "id") ??
            schema.CollectionId;

        var rows = pageElement.TryGetProperty("items", out var itemsElement) &&
                   itemsElement.ValueKind == JsonValueKind.Array
            ? itemsElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => MapGridRow(item, schema, collectionLabel))
                .ToArray()
            : [];

        return new WorkspaceArtifactDto
        {
            Kind = "grid",
            Title = collectionLabel,
            Grid = new WorkspaceGridDto
            {
                CollectionId = collectionId,
                CollectionLabel = collectionLabel,
                ResolvedFilters = [],
                Page = 0,
                PageSize = rows.Length,
                TotalCount = ReadLongProperty(pageElement, "count") ?? rows.Length,
                Columns = schema.Fields
                    .Select(field => new WorkspaceGridColumnDto
                    {
                        Key = field.Key,
                        Label = field.Label,
                        Type = field.Type
                    })
                    .ToArray(),
                Rows = rows
            }
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

    private static WorkspaceGridDto MapRecordsets(IEnumerable<RecordsetNameDto> recordsets)
    {
        var rows = recordsets.ToArray();
        return new WorkspaceGridDto
        {
            CollectionId = "recordsets",
            CollectionLabel = "Recordsets",
            ResolvedFilters = [],
            Page = 0,
            PageSize = rows.Length,
            TotalCount = rows.Length,
            Columns =
            [
                new WorkspaceGridColumnDto
                {
                    Key = "name",
                    Label = "Name",
                    Type = "text"
                },
                new WorkspaceGridColumnDto
                {
                    Key = "id",
                    Label = "Id",
                    Type = "text"
                },
                new WorkspaceGridColumnDto
                {
                    Key = "count",
                    Label = "Count",
                    Type = "number"
                }
            ],
            Rows = rows
                .Select(recordset => new WorkspaceGridRowDto
                {
                    EntityId = recordset.Id?.ToString() ?? string.Empty,
                    DisplayName = recordset.Name,
                    Attributes =
                    [
                        new WorkspaceAttributeDto
                        {
                            Key = "name",
                            Label = "Name",
                            Type = "text",
                            Value = recordset.Name,
                            DisplayValue = recordset.Name
                        },
                        new WorkspaceAttributeDto
                        {
                            Key = "id",
                            Label = "Id",
                            Type = "text",
                            Value = recordset.Id?.ToString(),
                            DisplayValue = recordset.Id?.ToString()
                        },
                        new WorkspaceAttributeDto
                        {
                            Key = "count",
                            Label = "Count",
                            Type = "number",
                            Value = recordset.Count,
                            DisplayValue = recordset.Count.ToString()
                        }
                    ],
                    AvailableActions = []
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

    private static WorkspaceEditorSchemaDto MapSchema(JsonElement definition)
    {
        var fields = definition.TryGetProperty("columns", out var columnsElement) &&
                     columnsElement.ValueKind == JsonValueKind.Array
            ? columnsElement.EnumerateArray()
                .Where(column => column.ValueKind == JsonValueKind.Object)
                .Select(column => new WorkspaceSchemaFieldDto
                {
                    Key = ReadStringProperty(column, "property") ??
                          ReadStringProperty(column, "key") ??
                          string.Empty,
                    Label = ReadStringProperty(column, "name") ??
                            ReadStringProperty(column, "property") ??
                            ReadStringProperty(column, "key") ??
                            string.Empty,
                    Type = MapFieldType(ReadStringProperty(column, "type") ?? "text"),
                    Required = ReadBooleanProperty(column, "required"),
                    AllowedValues = ReadStringArrayProperty(column, "allowedValues"),
                    ValidationHint = ReadStringProperty(column, "regex")
                })
                .Where(field => !string.IsNullOrWhiteSpace(field.Key))
                .ToList()
            : [];

        var statuses = definition.TryGetProperty("statuses", out var statusesElement) &&
                       statusesElement.ValueKind == JsonValueKind.Array
            ? statusesElement.EnumerateArray()
                .Select(status => ReadStringProperty(status, "name"))
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Cast<string>()
                .ToArray()
            : [];

        if (statuses.Length > 0)
        {
            fields.Add(new WorkspaceSchemaFieldDto
            {
                Key = "status",
                Label = "Status",
                Type = "enum",
                Required = true,
                AllowedValues = statuses
            });
        }

        var transitions = definition.TryGetProperty("transitions", out var transitionsElement) &&
                          transitionsElement.ValueKind == JsonValueKind.Array
            ? transitionsElement.EnumerateArray()
                .Where(transition => transition.ValueKind == JsonValueKind.Object)
                .Select(transition => new WorkspaceStateTransitionDto
                {
                    From = ReadStringProperty(transition, "from") ?? string.Empty,
                    AllowedNext = ReadStringArrayProperty(transition, "allowedNext")
                })
                .Where(transition => !string.IsNullOrWhiteSpace(transition.From))
                .ToArray()
            : [];

        return new WorkspaceEditorSchemaDto
        {
            CollectionId = ReadStringProperty(definition, "id") ?? string.Empty,
            CollectionLabel = ReadStringProperty(definition, "name") ?? string.Empty,
            Fields = fields.ToArray(),
            StateTransitions = transitions
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

    private static WorkspaceGridRowDto MapGridRow(
        JsonElement item,
        WorkspaceEditorSchemaDto schema,
        string collectionLabel
    )
    {
        item.TryGetProperty("bag", out var bagElement);
        var attributes = MapAttributes(
            bagElement.ValueKind == JsonValueKind.Object ? bagElement : default(JsonElement),
            schema);
        var entityId = item.TryGetProperty("id", out var idElement)
            ? idElement.ToString()
            : string.Empty;

        return new WorkspaceGridRowDto
        {
            EntityId = entityId,
            DisplayName = ResolveDisplayName(attributes, collectionLabel, entityId),
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

    private static JsonDocument? ParsePayloadDocument(string json)
    {
        var payloadJson = json;
        if (TryExtractPreferredPayload(json, out var extractedPayloadJson) &&
            !string.IsNullOrWhiteSpace(extractedPayloadJson))
        {
            payloadJson = extractedPayloadJson;
        }

        if (IsTriviallyEmptyJson(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(payloadJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind == JsonValueKind.String
            ? propertyValue.GetString()
            : propertyValue.ToString();
    }

    private static long? ReadLongProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        if (propertyValue.ValueKind == JsonValueKind.Number &&
            propertyValue.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        return long.TryParse(propertyValue.ToString(), out var parsedValue)
            ? parsedValue
            : null;
    }

    private static bool ReadBooleanProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return false;
        }

        return propertyValue.ValueKind == JsonValueKind.True ||
               (propertyValue.ValueKind == JsonValueKind.String &&
                bool.TryParse(propertyValue.GetString(), out var parsedValue) &&
                parsedValue);
    }

    private static string[] ReadStringArrayProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue) ||
            propertyValue.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return propertyValue.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
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
        if (IsTriviallyEmptyJson(json))
        {
            return default;
        }

        if (TryExtractPreferredPayload(json, out var wrappedPayloadJson))
        {
            return Deserialize<T>(wrappedPayloadJson);
        }

        if (TryDeserialize(json, out T? value))
        {
            return value;
        }

        return default;
    }

    private static bool IsTriviallyEmptyJson(string json)
    {
        return string.IsNullOrWhiteSpace(json) ||
               string.Equals(json.Trim(), "{}", StringComparison.Ordinal) ||
               string.Equals(json.Trim(), "null", StringComparison.OrdinalIgnoreCase);
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

    private static bool TryExtractPreferredPayload(string json, out string payloadJson)
    {
        payloadJson = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            return TryExtractPreferredPayload(document.RootElement, out payloadJson);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryExtractPreferredPayload(JsonElement element, out string payloadJson)
    {
        payloadJson = string.Empty;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                payloadJson = element.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(payloadJson);
            case JsonValueKind.Object:
                return TryExtractNamedWrapperProperty(element, out payloadJson);
            case JsonValueKind.Array:
                return TryExtractArrayPayload(element, out payloadJson);
            default:
                return false;
        }
    }

    private static bool TryExtractArrayPayload(JsonElement arrayElement, out string payloadJson)
    {
        payloadJson = string.Empty;
        string? textFallback = null;

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (TryExtractNamedWrapperProperty(item, out payloadJson))
            {
                return true;
            }

            if (textFallback is null &&
                item.TryGetProperty("text", out var textProperty) &&
                textProperty.ValueKind == JsonValueKind.String)
            {
                textFallback = textProperty.GetString();
            }
        }

        payloadJson = textFallback ?? string.Empty;
        return !string.IsNullOrWhiteSpace(payloadJson);
    }

    private static bool TryExtractNamedWrapperProperty(JsonElement element, out string payloadJson)
    {
        payloadJson = string.Empty;

        foreach (var propertyName in new[] { "structuredContent", "result", "value", "data" })
        {
            if (!element.TryGetProperty(propertyName, out var propertyValue))
            {
                continue;
            }

            payloadJson = ExtractElementPayload(propertyValue);
            if (!string.IsNullOrWhiteSpace(payloadJson))
            {
                return true;
            }
        }

        return false;
    }

    private static string ExtractElementPayload(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.GetRawText();
    }

    private static void Merge(JsonObject target, JsonObject source)
    {
        foreach (var pair in source)
        {
            target[pair.Key] = pair.Value?.DeepClone();
        }
    }

    private static string SummarizeRecordsets(string outputJson)
    {
        var artifact = TryMapRecordsetsArtifact(outputJson);
        return artifact?.Grid is null
            ? "Listed recordsets."
            : $"Found {artifact.Grid.Rows.Length} recordset(s).";
    }

    private static string SummarizeSchema(string outputJson)
    {
        var artifact = TryMapSchemaArtifact(outputJson);
        return artifact?.Editor is null
            ? "Resolved record schema."
            : $"Resolved the {artifact.Editor.CollectionLabel} schema.";
    }

    private static string SummarizeSearch(string outputJson)
    {
        var artifact = TryMapSearchArtifact(outputJson);
        return artifact?.Grid is null
            ? "Searched records."
            : $"Found {artifact.Grid.TotalCount} result(s) in {artifact.Grid.CollectionLabel}.";
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
