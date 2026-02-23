using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Infrastructure.Sql.QueryCriteria;

namespace Records.Recordsets.Infrastructure.Sql;

public sealed class RecordsetQueries(RecordsetsDbContext dbContext) : IRecordsetQueries
{
    public async Task<RecordsetSummaryDto?> GetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetProjections
            .AsNoTracking()
            .ApplyCriteria(new RecordsetProjectionByRecordsetIdCriteria(recordsetKey))
            .Select(x => new RecordsetSummaryDto(
                UlidId.Parse(x.RecordsetId),
                x.Name,
                x.ItemCount,
                x.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordsetSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RecordsetProjections
            .AsNoTracking()
            .ApplyCriteria(new RecordsetProjectionsOrderedByNameCriteria())
            .Select(x => new RecordsetSummaryDto(
                UlidId.Parse(x.RecordsetId),
                x.Name,
                x.ItemCount,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordsetNameDto>> ListNamesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RecordsetProjections
            .AsNoTracking()
            .ApplyCriteria(new RecordsetProjectionsOrderedByNameCriteria())
            .Select(x => new RecordsetNameDto
            {
                Id = UlidId.Parse(x.RecordsetId),
                Name = x.Name,
                Count = x.ItemCount
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RecordsetItemDefinitionDto?> GetItemDefinitionAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken
    )
    {
        var recordsetKey = recordsetId.ToString();
        var recordset = await dbContext.Recordsets
            .AsNoTracking()
            .ApplyCriteria(new RecordsetByIdCriteria(recordsetKey))
            .FirstOrDefaultAsync(cancellationToken);
        if (recordset is null)
        {
            return null;
        }

        var columnRows = await dbContext.RecordsetColumns
            .AsNoTracking()
            .ApplyCriteria(new RecordsetColumnsByRecordsetIdCriteria(recordsetKey))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var columns = columnRows
            .Select(x => new RecordsetColumnDto
            {
                StorageKey = x.StorageKey,
                Name = x.Name,
                Property = ComputeProperty(x.Name),
                Type = x.Type,
                Required = x.Required,
                AllowedValues = ParseStringArray(x.AllowedValuesJson),
                MinNumber = x.MinNumber,
                MaxNumber = x.MaxNumber,
                Regex = x.Regex
            })
            .ToArray();

        var statuses = await dbContext.RecordsetStatuses
            .AsNoTracking()
            .ApplyCriteria(new RecordsetStatusesByRecordsetIdCriteria(recordsetKey))
            .OrderBy(x => x.Id)
            .Select(x => new RecordsetStatusDto
            {
                Name = x.Name,
                Color = x.Color
            })
            .ToArrayAsync(cancellationToken);

        var transitionRows = await dbContext.RecordsetStatusTransitions
            .AsNoTracking()
            .ApplyCriteria(new RecordsetStatusTransitionsByRecordsetIdCriteria(recordsetKey))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var transitions = transitionRows
            .Select(x => new RecordsetStatusTransitionDto
            {
                From = x.From,
                AllowedNext = ParseStringArray(x.AllowedNextJson) ?? []
            })
            .ToArray();

        return new RecordsetItemDefinitionDto
        {
            Id = UlidId.Parse(recordset.Id),
            Name = recordset.Name,
            Columns = columns,
            Statuses = statuses,
            Transitions = transitions
        };
    }

    private static string[]? ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeProperty(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var words = name
            .Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        var first = words[0];
        var property = first[..1].ToLowerInvariant() + first[1..];
        for (var i = 1; i < words.Length; i++)
        {
            var word = words[i];
            property += word[..1].ToUpperInvariant() + word[1..];
        }

        return property;
    }
}