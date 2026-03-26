using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain.Events;
using Records.Recordsets.Infrastructure.Sql.QueryCriteria;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordQueries(
    RecordsetsDbContext dbContext,
    IEventStore eventStore,
    IDomainEventSerializer serializer,
    ITenantContext? tenantContext = null
) : IRecordQueries
{
    private static readonly Regex FieldPathRegex = new(
        "^[A-Za-z0-9_]+(\\.[A-Za-z0-9_]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<RecordDto?> GetByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .ApplyCriteria(new RecordsetItemByRecordsetIdAndIdCriteria(recordsetKey, recordId))
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordDto>> ListAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .ApplyCriteria(new RecordsetItemsByRecordsetIdCriteria(recordsetKey))
            .OrderByDescending(x => x.Id)
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecordsetPagedRecordsDto?> GetPageAsync(
        UlidId recordsetId,
        int page,
        int pageSize,
        string? status,
        string? field,
        string? sort,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var builder = new SqlBuilder();
        var parameters = new DynamicParameters();
        parameters.Add("recordsetId", recordsetKey);
        parameters.Add("pageSize", pageSize);
        parameters.Add("offset", page * pageSize);
        const string sql = """
                           SELECT SQL_CALC_FOUND_ROWS
                               i.BagJson, i.Id, i.RecordsetId
                           FROM
                               RecordsetItems i
                           WHERE
                               i.RecordsetId = @recordsetId
                           /**where**/
                           /**orderby**/
                           LIMIT @pageSize OFFSET @offset;
                           SELECT FOUND_ROWS();
                           SELECT Name FROM Recordsets WHERE Id = @recordsetId;
                           """;

        var template = builder.AddTemplate(sql, parameters);
        if (!string.IsNullOrWhiteSpace(status))
        {
            builder.Where("JSON_UNQUOTE(JSON_EXTRACT(i.BagJson, '$.\"status\"')) = @status");
            parameters.Add("status", status.Trim());
        }

        builder.OrderBy(BuildOrderByClause(field, sort));

        var connection = dbContext.Database.GetDbConnection();
        var command = new CommandDefinition(
            template.RawSql,
            template.Parameters,
            cancellationToken: cancellationToken);
        var multi = await connection.QueryMultipleAsync(command);

        var rows = await multi.ReadAsync<RecordPageRow>();
        var items = rows
            .Select(row => new RecordListItemDto
            {
                Id = (int)row.Id,
                RecordsetId = UlidId.Parse(row.RecordsetId),
                Bag = DeserializeBag(row.BagJson)
            })
            .ToArray();

        var count = await multi.ReadSingleAsync<long>();
        var recordsetName = await multi.ReadSingleOrDefaultAsync<string?>();
        if (recordsetName is null)
        {
            return null;
        }

        return new RecordsetPagedRecordsDto
        {
            RecordsetId = recordsetId,
            Name = recordsetName,
            Count = count,
            Items = items
        };
    }

    public async Task<RecordItemDetailsDto?> GetDetailsAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var item = await dbContext.RecordsetItems
            .AsNoTracking()
            .ApplyCriteria(new RecordsetItemByRecordsetIdAndIdCriteria(recordsetKey, recordId))
            .Select(x => new { x.Id, x.BagJson })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        return new RecordItemDetailsDto
        {
            Id = (int)item.Id,
            RecordsetId = recordsetId,
            Bag = DeserializeBag(item.BagJson)
        };
    }

    public async Task<HistoryPageDto> GetRecordsetHistoryAsync(
        UlidId recordsetId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var ordered = (await LoadRecordsetHistoryAsync(recordsetId, cancellationToken))
            .OrderByDescending(x => x.On)
            .ToArray();
        return new HistoryPageDto
        {
            Items = ordered.Skip(page * pageSize).Take(pageSize).ToArray(),
            Page = page,
            PageSize = pageSize,
            Total = ordered.Length
        };
    }

    public async Task<HistoryPageDto?> GetRecordHistoryAsync(
        UlidId recordsetId,
        int recordId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var activities = await dbContext.RecordActivities
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey && x.RecordId == recordId)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        if (activities.Count == 0)
        {
            return null;
        }

        var entries = activities
            .Select(activity => new HistoryEntryDto
            {
                Type = activity.ActionType,
                On = activity.OccurredAt,
                By = activity.ActorId,
                Bag = DeserializeBag(activity.BagJson ?? "{}")
            })
            .ToArray();
        return new HistoryPageDto
        {
            Items = entries.Skip(page * pageSize).Take(pageSize).ToArray(),
            Page = page,
            PageSize = pageSize,
            Total = entries.Length
        };
    }

    private static JsonElement DeserializeBag(string bagJson)
    {
        if (string.IsNullOrWhiteSpace(bagJson))
        {
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(bagJson);
        }
        catch
        {
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }
    }

    private static string BuildOrderByClause(string? field, string? sort)
    {
        var direction = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        if (string.IsNullOrWhiteSpace(field))
        {
            return $"i.Id {direction}";
        }

        var normalizedField = field.Trim();
        if (normalizedField.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return $"i.Id {direction}";
        }

        if (!FieldPathRegex.IsMatch(normalizedField))
        {
            return $"i.Id {direction}";
        }

        var path = "$." + string.Join(
            ".",
            normalizedField
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => $"\"{segment}\""));

        return $"JSON_EXTRACT(i.BagJson, '{path}') {direction}, i.Id {direction}";
    }

    private async Task<IReadOnlyList<HistoryEntryDto>> LoadRecordsetHistoryAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken
    )
    {
        var events = new List<StoredEvent>();
        var tenantId = string.IsNullOrWhiteSpace(tenantContext?.TenantId)
            ? "*"
            : tenantContext!.TenantId;
        var streamId = $"Recordset:{recordsetId}";
        long position = 0;

        while (true)
        {
            var batch = await eventStore.ReadStreamAsync(tenantId, streamId, position, 128, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            events.AddRange(batch);
            position = batch[^1].Position;
        }

        return events
            .Select(ToRecordsetHistoryEntry)
            .Where(entry => entry is not null)
            .Cast<HistoryEntryDto>()
            .ToArray();
    }

    private HistoryEntryDto? ToRecordsetHistoryEntry(StoredEvent storedEvent)
    {
        var domainEvent = serializer.Deserialize(storedEvent.Payload, storedEvent.EventName);
        return domainEvent switch
        {
            RecordsetCreated created => new HistoryEntryDto
            {
                Type = "Created",
                On = ToOffset(storedEvent.CreatedAt),
                By = storedEvent.ActorId,
                Bag = new { name = created.Name }
            },
            RecordsetUpdated => new HistoryEntryDto
            {
                Type = "Updated",
                On = ToOffset(storedEvent.CreatedAt),
                By = storedEvent.ActorId
            },
            _ => null
        };
    }

    private static DateTimeOffset ToOffset(DateTime createdAt)
    {
        var utcValue = createdAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(createdAt, DateTimeKind.Utc)
            : createdAt.ToUniversalTime();

        return new DateTimeOffset(utcValue);
    }

    private sealed class RecordPageRow
    {
        public long Id { get; init; }
        public string RecordsetId { get; } = string.Empty;
        public string BagJson { get; } = string.Empty;
    }
}