using System.Text.Json;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain;

namespace Records.Recordsets.McpServer;

internal sealed class RecordsetsMcpTools(
    IRecordsetQueries recordsetQueries,
    IRecordQueries recordQueries,
    IRecordsetsUnitOfWork unitOfWork,
    IRecordBagValidator bagValidator,
    IMediator mediator,
    McpTenantContextAccessor tenantContextAccessor
)
{
    [McpServerTool(
        Name = "list_recordsets",
        Title = "List recordsets",
        ReadOnly = true,
        UseStructuredContent = true)]
    public async Task<IReadOnlyList<RecordsetNameDto>> ListRecordsetsAsync(
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        return await recordsetQueries.ListNamesAsync(cancellationToken);
    }

    [McpServerTool(
        Name = "resolve_record_schema",
        Title = "Resolve record schema",
        ReadOnly = true,
        UseStructuredContent = true)]
    public async Task<RecordsetItemDefinitionDto?> ResolveRecordSchemaAsync(
        string collectionId,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId))
        {
            return null;
        }

        return await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
    }

    [McpServerTool(
        Name = "search_records",
        Title = "Search records",
        ReadOnly = true,
        UseStructuredContent = true)]
    public async Task<RecordsetSearchToolResultDto?> SearchRecordsAsync(
        string collectionId,
        int page,
        int pageSize,
        RecordSearchFilterClause[]? filters,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId))
        {
            return null;
        }

        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (schema is null)
        {
            return null;
        }

        var pageResult = await recordQueries.SearchAsync(
            recordsetId,
            page,
            Math.Clamp(pageSize, 1, 50),
            filters ?? [],
            cancellationToken);
        if (pageResult is null)
        {
            return null;
        }

        return new RecordsetSearchToolResultDto
        {
            Schema = schema,
            Page = pageResult
        };
    }

    [McpServerTool(
        Name = "get_record",
        Title = "Get record details",
        ReadOnly = true,
        UseStructuredContent = true)]
    public async Task<RecordsetDetailToolResultDto?> GetRecordAsync(
        string collectionId,
        int entityId,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId) || entityId <= 0)
        {
            return null;
        }

        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var detail = await recordQueries.GetDetailsAsync(recordsetId, entityId, cancellationToken);
        if (schema is null || detail is null)
        {
            return null;
        }

        return new RecordsetDetailToolResultDto
        {
            Schema = schema,
            Detail = detail
        };
    }

    [McpServerTool(
        Name = "get_record_history",
        Title = "Get record history",
        ReadOnly = true,
        UseStructuredContent = true)]
    public async Task<RecordsetHistoryToolResultDto?> GetRecordHistoryAsync(
        string collectionId,
        int entityId,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId) || entityId <= 0)
        {
            return null;
        }

        var history = await recordQueries.GetRecordHistoryAsync(recordsetId, entityId, 0, 20, cancellationToken);
        if (history is null)
        {
            return null;
        }

        return new RecordsetHistoryToolResultDto
        {
            RecordsetId = recordsetId,
            RecordId = entityId,
            History = history
        };
    }

    [McpServerTool(
        Name = "validate_record_create",
        Title = "Validate record create",
        UseStructuredContent = true)]
    public async Task<RecordMutationProposalDto?> ValidateRecordCreateAsync(
        string collectionId,
        JsonElement changes,
        string? sourceExcerpt,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId))
        {
            return null;
        }

        var changeDictionary = ReadChanges(changes);
        if (changeDictionary.Count == 0)
        {
            return null;
        }

        var recordset = await unitOfWork.GetRecordsetByIdAsync(recordsetId, cancellationToken);
        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (recordset is null || schema is null)
        {
            return null;
        }

        var currentBag = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var proposedBag = new Dictionary<string, object?>(changeDictionary, StringComparer.OrdinalIgnoreCase);

        bagValidator.Validate(recordset, proposedBag);

        return new RecordMutationProposalDto
        {
            Kind = "create",
            ProposalId = UlidId.NewUlid().ToString(),
            RecordsetId = recordsetId,
            RecordsetName = schema.Name,
            Schema = schema,
            Current = new RecordItemDetailsDto
            {
                RecordsetId = recordsetId,
                Bag = currentBag
            },
            Proposed = new RecordItemDetailsDto
            {
                RecordsetId = recordsetId,
                Bag = proposedBag
            },
            Diffs = BuildDiffs(currentBag, proposedBag, schema),
            Rationale = "Prepared from the requested field values.",
            SourceExcerpt = sourceExcerpt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    [McpServerTool(
        Name = "validate_record_update",
        Title = "Validate record update",
        UseStructuredContent = true)]
    public async Task<RecordMutationProposalDto?> ValidateRecordUpdateAsync(
        string collectionId,
        int entityId,
        JsonElement changes,
        string? sourceExcerpt,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId) || entityId <= 0)
        {
            return null;
        }

        var changeDictionary = ReadChanges(changes);
        if (changeDictionary.Count == 0)
        {
            return null;
        }

        var recordset = await unitOfWork.GetRecordsetByIdAsync(recordsetId, cancellationToken);
        var record = await unitOfWork.GetRecordByIdAsync(recordsetId, entityId, cancellationToken);
        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        if (recordset is null || record is null || schema is null)
        {
            return null;
        }

        var currentBag = ToDictionary(record.Bag);
        var proposedBag = new Dictionary<string, object?>(currentBag, StringComparer.OrdinalIgnoreCase);
        foreach (var change in changeDictionary)
        {
            proposedBag[change.Key] = change.Value;
        }

        bagValidator.Validate(recordset, proposedBag);
        bagValidator.ValidateTransition(recordset, currentBag, proposedBag);

        return new RecordMutationProposalDto
        {
            Kind = "update",
            ProposalId = UlidId.NewUlid().ToString(),
            RecordsetId = recordsetId,
            RecordsetName = schema.Name,
            Schema = schema,
            Current = new RecordItemDetailsDto
            {
                Id = record.Id,
                RecordsetId = record.RecordsetId,
                Bag = currentBag
            },
            Proposed = new RecordItemDetailsDto
            {
                Id = record.Id,
                RecordsetId = record.RecordsetId,
                Bag = proposedBag
            },
            Diffs = BuildDiffs(currentBag, proposedBag, schema),
            Rationale = "Prepared from the requested field changes.",
            SourceExcerpt = sourceExcerpt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    [McpServerTool(
        Name = "apply_record_create",
        Title = "Apply record create",
        Idempotent = false,
        UseStructuredContent = true)]
    public async Task<RecordsetApplyToolResultDto?> ApplyRecordCreateAsync(
        string collectionId,
        JsonElement changes,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId))
        {
            return null;
        }

        var changeDictionary = ReadChanges(changes);
        if (changeDictionary.Count == 0)
        {
            return null;
        }

        var result = await mediator.Send(new CreateRecordCommand(recordsetId, changeDictionary), cancellationToken);
        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var detail = await recordQueries.GetDetailsAsync(recordsetId, result.RecordId, cancellationToken);
        if (schema is null || detail is null)
        {
            return null;
        }

        return new RecordsetApplyToolResultDto
        {
            Schema = schema,
            Detail = detail
        };
    }

    [McpServerTool(
        Name = "apply_record_update",
        Title = "Apply record update",
        Idempotent = false,
        UseStructuredContent = true)]
    public async Task<RecordsetApplyToolResultDto?> ApplyRecordUpdateAsync(
        string collectionId,
        int entityId,
        JsonElement changes,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken
    )
    {
        using var scope = tenantContextAccessor.Push(requestContext.Params.Meta);
        if (!TryParseRecordsetId(collectionId, out var recordsetId) || entityId <= 0)
        {
            return null;
        }

        var record = await unitOfWork.GetRecordByIdAsync(recordsetId, entityId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var changeDictionary = ReadChanges(changes);
        if (changeDictionary.Count == 0)
        {
            return null;
        }

        var nextBag = ToDictionary(record.Bag);
        foreach (var change in changeDictionary)
        {
            nextBag[change.Key] = change.Value;
        }

        await mediator.Send(new UpdateRecordCommand(recordsetId, entityId, nextBag), cancellationToken);
        var schema = await recordsetQueries.GetItemDefinitionAsync(recordsetId, cancellationToken);
        var detail = await recordQueries.GetDetailsAsync(recordsetId, entityId, cancellationToken);
        if (schema is null || detail is null)
        {
            return null;
        }

        return new RecordsetApplyToolResultDto
        {
            Schema = schema,
            Detail = detail
        };
    }

    private static RecordMutationDiffDto[] BuildDiffs(
        IReadOnlyDictionary<string, object?> currentBag,
        IReadOnlyDictionary<string, object?> proposedBag,
        RecordsetItemDefinitionDto schema
    )
    {
        return EnumerateFieldMetadata(schema)
            .Select(field =>
            {
                currentBag.TryGetValue(field.Key, out var before);
                proposedBag.TryGetValue(field.Key, out var after);
                return new RecordMutationDiffDto
                {
                    Key = field.Key,
                    Label = field.Label,
                    Before = before,
                    After = after
                };
            })
            .Where(diff => !Equals(diff.Before?.ToString(), diff.After?.ToString()))
            .ToArray();
    }

    private static IEnumerable<(string Key, string Label)> EnumerateFieldMetadata(RecordsetItemDefinitionDto schema)
    {
        foreach (var column in schema.Columns)
        {
            yield return (column.Property, column.Name);
        }

        yield return ("status", "Status");
    }

    private static Dictionary<string, object?> ReadChanges(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in value.EnumerateObject())
        {
            result[property.Name] = ReadJsonValue(property.Value);
        }

        return result;
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

    private static bool TryParseRecordsetId(string collectionId, out UlidId recordsetId)
    {
        return UlidId.TryParse(collectionId, out recordsetId);
    }
}