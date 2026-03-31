using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Infrastructure.Sql;

public sealed partial class RecordsBackendAdapter(
    IEnumerable<IAgentModuleServer> moduleServers
) : IWorkspaceBackendAdapter
{
    public string Id => "records";

    public async Task<WorkspaceGridDto?> SearchAsync(
        WorkspaceSearchRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var recordsetsServer = GetRecordsetsServer();
        var recordsets = await InvokeAsync(
            recordsetsServer,
            "list_recordsets",
            new Dictionary<string, object?>(),
            cancellationToken) as JsonArray;

        var collection = ResolveCollection(recordsets, request.Query, request.CurrentArtifact);
        if (collection is null)
        {
            return null;
        }

        var collectionId = collection["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            return null;
        }

        var schema = await ResolveSchemaAsync(new WorkspaceResolveSchemaRequestDto
        {
            CollectionId = collectionId
        }, cancellationToken);
        if (schema is null)
        {
            return null;
        }

        var resolvedFilters = ResolveFilters(request.Query, schema);
        var resultNode = await InvokeAsync(
            recordsetsServer,
            "search_records",
            new Dictionary<string, object?>
            {
                ["collectionId"] = collectionId,
                ["page"] = 0,
                ["pageSize"] = 20,
                ["filters"] = resolvedFilters
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceGridDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceEntityDto?> GetEntityAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var resultNode = await InvokeAsync(
            GetRecordsetsServer(),
            "get_record",
            new Dictionary<string, object?>
            {
                ["collectionId"] = request.CollectionId,
                ["entityId"] = request.EntityId
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceEntityDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceHistoryDto?> GetHistoryAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var resultNode = await InvokeAsync(
            GetRecordsetsServer(),
            "get_record_history",
            new Dictionary<string, object?>
            {
                ["collectionId"] = request.CollectionId,
                ["entityId"] = request.EntityId
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceHistoryDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceEditorSchemaDto?> ResolveSchemaAsync(
        WorkspaceResolveSchemaRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var resultNode = await InvokeAsync(
            GetRecordsetsServer(),
            "resolve_record_schema",
            new Dictionary<string, object?>
            {
                ["collectionId"] = request.CollectionId
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceEditorSchemaDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceProposalDto?> ProposeUpdateAsync(
        WorkspaceProposeUpdateRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var detail = request.CurrentArtifact?.Detail;
        if (detail is null)
        {
            var entityRequest = ResolveEntityRequestFromPrompt(request.Prompt, request.CurrentArtifact);
            if (entityRequest is null)
            {
                return null;
            }

            detail = await GetEntityAsync(entityRequest, cancellationToken);
        }

        if (detail is null)
        {
            return null;
        }

        var schema = detail.Schema ?? await ResolveSchemaAsync(new WorkspaceResolveSchemaRequestDto
        {
            CollectionId = detail.CollectionId
        }, cancellationToken);

        if (schema is null)
        {
            return null;
        }

        var changes = ExtractChanges(request.Prompt, request.PastedText, schema);
        if (changes.Count == 0)
        {
            return null;
        }

        var resultNode = await InvokeAsync(
            GetRecordsetsServer(),
            "validate_record_update",
            new Dictionary<string, object?>
            {
                ["collectionId"] = detail.CollectionId,
                ["entityId"] = detail.EntityId,
                ["changes"] = changes,
                ["sourceExcerpt"] = request.PastedText
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceProposalDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceProposalDto?> ProposeCreateAsync(
        WorkspaceProposeCreateRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var recordsetsServer = GetRecordsetsServer();
        var recordsets = await InvokeAsync(
            recordsetsServer,
            "list_recordsets",
            new Dictionary<string, object?>(),
            cancellationToken) as JsonArray;

        var collection = ResolveCollection(recordsets, request.Prompt, request.CurrentArtifact);
        if (collection is null)
        {
            return null;
        }

        var collectionId = collection["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            return null;
        }

        var schema = await ResolveSchemaAsync(new WorkspaceResolveSchemaRequestDto
        {
            CollectionId = collectionId
        }, cancellationToken);
        if (schema is null)
        {
            return null;
        }

        var changes = ExtractChanges(request.Prompt, request.PastedText, schema);
        ApplyCreateDefaults(changes, schema);
        if (changes.Count == 0)
        {
            return null;
        }

        var resultNode = await InvokeAsync(
            recordsetsServer,
            "validate_record_create",
            new Dictionary<string, object?>
            {
                ["collectionId"] = collectionId,
                ["changes"] = changes,
                ["sourceExcerpt"] = request.PastedText
            },
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceProposalDto>(resultNode, AgentJsonSerializer.Options);
    }

    public async Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        WorkspaceApplyProposalRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var changes = request.Proposal.Diffs
            .ToDictionary(x => x.Key, x => x.After);

        var resultNode = await InvokeAsync(
            GetRecordsetsServer(),
            string.Equals(request.Proposal.Kind, "create", StringComparison.OrdinalIgnoreCase)
                ? "apply_record_create"
                : "apply_record_update",
            BuildApplyArguments(request.Proposal, changes),
            cancellationToken);

        return resultNode is null
            ? null
            : JsonSerializer.Deserialize<WorkspaceEntityDto>(resultNode, AgentJsonSerializer.Options);
    }

    public Task<WorkspaceNotificationsDto?> GetContextNotificationsAsync(
        WorkspaceEntityRequestDto request,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult<WorkspaceNotificationsDto?>(null);
    }

    private static JsonObject? ResolveCollection(
        JsonArray? collections,
        string prompt,
        WorkspaceArtifactDto? currentArtifact
    )
    {
        if (collections is null || collections.Count == 0)
        {
            return null;
        }

        var currentCollectionId = currentArtifact?.Detail?.CollectionId ?? currentArtifact?.Grid?.CollectionId;
        if (!string.IsNullOrWhiteSpace(currentCollectionId))
        {
            var currentCollection = collections
                .OfType<JsonObject>()
                .FirstOrDefault(node => string.Equals(
                    node["id"]?.GetValue<string>(),
                    currentCollectionId,
                    StringComparison.Ordinal));

            if (currentCollection is not null)
            {
                return currentCollection;
            }
        }

        var normalizedPrompt = prompt.Trim().ToLowerInvariant();
        JsonObject? bestMatch = null;
        var bestScore = -1;

        foreach (var node in collections.OfType<JsonObject>())
        {
            var name = node["name"]?.GetValue<string>() ?? string.Empty;
            var normalizedName = name.Trim().ToLowerInvariant();
            var score = 0;

            if (!string.IsNullOrWhiteSpace(normalizedName) && normalizedPrompt.Contains(normalizedName, StringComparison.Ordinal))
            {
                score = normalizedName.Length + 10;
            }
            else
            {
                var singular = normalizedName.EndsWith("s", StringComparison.Ordinal)
                    ? normalizedName[..^1]
                    : normalizedName;
                if (!string.IsNullOrWhiteSpace(singular) &&
                    normalizedPrompt.Contains(singular, StringComparison.Ordinal))
                {
                    score = singular.Length + 5;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = node;
            }
        }

        if (bestMatch is not null && bestScore > 0)
        {
            return bestMatch;
        }

        return collections.Count == 1 ? collections[0] as JsonObject : null;
    }

    private static List<Dictionary<string, object?>> ResolveFilters(string prompt, WorkspaceEditorSchemaDto schema)
    {
        var filters = new List<Dictionary<string, object?>>();
        var normalizedPrompt = prompt.ToLowerInvariant();

        if (normalizedPrompt.Contains("yesterday", StringComparison.Ordinal))
        {
            var dateField = schema.Fields.FirstOrDefault(field => field.Type == "date");
            if (dateField is not null)
            {
                var day = DateTimeOffset.UtcNow.Date.AddDays(-1);
                filters.Add(new Dictionary<string, object?>
                {
                    ["field"] = dateField.Key,
                    ["operator"] = "on_or_after",
                    ["value"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
                filters.Add(new Dictionary<string, object?>
                {
                    ["field"] = dateField.Key,
                    ["operator"] = "on_or_before",
                    ["value"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
            }
        }

        var forMatch = ForValueRegex().Match(prompt);
        if (forMatch.Success)
        {
            var value = forMatch.Groups["value"].Value.Trim();
            var field = schema.Fields.FirstOrDefault(candidate =>
                candidate.Type == "text" &&
                (candidate.Key.Contains("client", StringComparison.OrdinalIgnoreCase) ||
                 candidate.Key.Contains("customer", StringComparison.OrdinalIgnoreCase) ||
                 candidate.Label.Contains("client", StringComparison.OrdinalIgnoreCase) ||
                 candidate.Label.Contains("customer", StringComparison.OrdinalIgnoreCase)));

            if (field is not null && value.Length > 0)
            {
                filters.Add(new Dictionary<string, object?>
                {
                    ["field"] = field.Key,
                    ["operator"] = "contains",
                    ["value"] = value
                });
            }
        }

        return filters;
    }

    private static void ApplyCreateDefaults(
        IDictionary<string, object?> changes,
        WorkspaceEditorSchemaDto schema
    )
    {
        var statusField = schema.Fields.FirstOrDefault(field =>
            field.Key.Equals("status", StringComparison.OrdinalIgnoreCase));
        if (statusField is null || statusField.AllowedValues.Length == 0)
        {
            return;
        }

        if (!changes.ContainsKey(statusField.Key))
        {
            changes[statusField.Key] = statusField.AllowedValues[0];
        }
    }

    private static Dictionary<string, object?> ExtractChanges(
        string prompt,
        string? pastedText,
        WorkspaceEditorSchemaDto schema
    )
    {
        var changes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var setMatch = SetFieldRegex().Match(prompt);
        if (setMatch.Success)
        {
            var requestedField = setMatch.Groups["field"].Value.Trim();
            var requestedValue = setMatch.Groups["value"].Value.Trim();
            var field = FindField(schema, requestedField);
            if (field is not null)
            {
                changes[field.Key] = requestedValue;
            }
        }

        var statusMatch = StatusRegex().Match(prompt);
        if (statusMatch.Success)
        {
            var requestedValue = statusMatch.Groups["value"].Value.Trim();
            var statusField = FindField(schema, "status");
            if (statusField is not null)
            {
                changes[statusField.Key] = requestedValue;
            }
        }

        if (!string.IsNullOrWhiteSpace(pastedText))
        {
            foreach (Match match in PastedLineRegex().Matches(pastedText))
            {
                var requestedField = match.Groups["field"].Value.Trim();
                var requestedValue = match.Groups["value"].Value.Trim();
                var field = FindField(schema, requestedField);
                if (field is not null && requestedValue.Length > 0)
                {
                    changes[field.Key] = requestedValue;
                }
            }
        }

        return changes;
    }

    private static WorkspaceSchemaFieldDto? FindField(WorkspaceEditorSchemaDto schema, string requestedField)
    {
        var normalized = requestedField.Trim();
        return schema.Fields.FirstOrDefault(field =>
            field.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            field.Label.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static WorkspaceEntityRequestDto? ResolveEntityRequestFromPrompt(
        string prompt,
        WorkspaceArtifactDto? currentArtifact
    )
    {
        var entityIdMatch = EntityIdRegex().Match(prompt);
        var entityId = entityIdMatch.Success ? entityIdMatch.Groups["id"].Value : null;
        var collectionId = currentArtifact?.Detail?.CollectionId ?? currentArtifact?.Grid?.CollectionId;

        if (string.IsNullOrWhiteSpace(collectionId) || string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        return new WorkspaceEntityRequestDto
        {
            CollectionId = collectionId,
            EntityId = entityId
        };
    }

    private static Dictionary<string, object?> BuildApplyArguments(
        WorkspaceProposalDto proposal,
        Dictionary<string, object?> changes
    )
    {
        var arguments = new Dictionary<string, object?>
        {
            ["collectionId"] = proposal.Target.CollectionId,
            ["changes"] = changes
        };

        if (!string.Equals(proposal.Kind, "create", StringComparison.OrdinalIgnoreCase))
        {
            arguments["entityId"] = proposal.Target.EntityId;
        }

        return arguments;
    }

    private IAgentModuleServer GetRecordsetsServer()
    {
        return moduleServers.First(server => server.ModuleId == "recordsets");
    }

    private static Task<JsonNode?> InvokeAsync(
        IAgentModuleServer server,
        string operation,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        return server.InvokeAsync(operation, arguments, cancellationToken);
    }

    [GeneratedRegex(@"\bfor\s+(?<value>.+?)(?:\s+yesterday|\s+today|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForValueRegex();

    [GeneratedRegex(@"\bset\s+(?<field>[\w\s]+?)\s+to\s+(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SetFieldRegex();

    [GeneratedRegex(@"\bstatus\s+(?:to|as)\s+(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"^(?<field>[\w\s]+)\s*:\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex PastedLineRegex();

    [GeneratedRegex(@"\b(?:record|order)\s+(?<id>\d+)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EntityIdRegex();
}
