using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Infrastructure.Sql.Queries;

public sealed class RecordQueries(RecordsetsDbContext dbContext) : IRecordQueries
{
    private static readonly Regex FieldPathRegex = new(
        "^[A-Za-z0-9_]+(\\.[A-Za-z0-9_]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<RecordDto?> GetByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey && x.Id == recordId)
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordDto>> ListAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecordsetPagedRecordsDto?> GetPageAsync(
        UlidId recordsetId,
        int page,
        int pageSize,
        string? field,
        string? sort,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var builder = new SqlBuilder();
        const string sql = """
                           SELECT SQL_CALC_FOUND_ROWS
                               i.BagJson, i.Id, i.RecordsetId
                           FROM
                               recordset_items i
                           WHERE
                               i.RecordsetId = @recordsetId
                           /**orderby**/
                           LIMIT @pageSize OFFSET @offset;
                           SELECT FOUND_ROWS();
                           SELECT Name FROM recordsets WHERE Id = @recordsetId;
                           """;

        var parameters = new
        {
            recordsetId = recordsetKey,
            pageSize,
            offset = page * pageSize
        };

        var template = builder.AddTemplate(sql, parameters);
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
            .Where(x => x.RecordsetId == recordsetKey && x.Id == recordId)
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
        var recordsetKey = recordsetId.ToString();
        var recordset = await dbContext.Recordsets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == recordsetKey, cancellationToken);
        if (recordset is null)
        {
            return new HistoryPageDto
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                Total = 0
            };
        }

        var entries = new List<HistoryEntryDto>
        {
            new()
            {
                Type = "Created",
                On = recordset.CreatedAt,
                By = recordset.CreatedBy,
                Bag = new { name = recordset.Name }
            }
        };

        if (recordset.UpdatedAt.HasValue)
        {
            entries.Add(new HistoryEntryDto
            {
                Type = "Updated",
                On = recordset.UpdatedAt.Value,
                By = recordset.UpdatedBy,
                Bag = new { name = recordset.Name }
            });
        }

        var ordered = entries.OrderByDescending(x => x.On).ToArray();
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
        var item = await dbContext.RecordsetItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RecordsetId == recordsetKey && x.Id == recordId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var entries = new List<HistoryEntryDto>
        {
            new()
            {
                Type = "Created",
                On = item.CreatedAt,
                By = item.CreatedBy,
                Bag = DeserializeBag(item.BagJson)
            }
        };

        if (item.UpdatedAt.HasValue)
        {
            entries.Add(new HistoryEntryDto
            {
                Type = "Updated",
                On = item.UpdatedAt.Value,
                By = item.UpdatedBy,
                Bag = DeserializeBag(item.BagJson)
            });
        }

        var ordered = entries.OrderByDescending(x => x.On).ToArray();
        return new HistoryPageDto
        {
            Items = ordered.Skip(page * pageSize).Take(pageSize).ToArray(),
            Page = page,
            PageSize = pageSize,
            Total = ordered.Length
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

    private sealed class RecordPageRow
    {
        public long Id { get; init; }
        public string RecordsetId { get; init; } = string.Empty;
        public string BagJson { get; init; } = string.Empty;
    }
}
