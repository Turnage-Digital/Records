using System.Text.Json;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.ValueObjects;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Mappers;

public static class RecordsetMapper
{
    public static Recordset ToDomain(
        RecordsetDb recordset,
        IReadOnlyList<RecordsetColumnDb> columns,
        IReadOnlyList<RecordsetStatusDb> statuses,
        IReadOnlyList<RecordsetStatusTransitionDb> transitions
    )
    {
        var domain = new Recordset(
            UlidId.Parse(recordset.Id),
            recordset.Name,
            UlidId.Parse(recordset.CreatedBy),
            recordset.CreatedAt);

        domain.LoadSchema(
            columns.Select(ToColumn).ToList(),
            statuses.Select(ToStatus).ToList(),
            transitions.Select(ToTransition).ToList(),
            recordset.UpdatedBy is null ? null : UlidId.Parse(recordset.UpdatedBy),
            recordset.UpdatedAt ?? recordset.CreatedAt
        );

        return domain;
    }

    public static RecordsetDb ToDb(Recordset recordset)
    {
        return new RecordsetDb
        {
            Id = recordset.Id.ToString(),
            Name = recordset.Name,
            CreatedBy = recordset.CreatedBy.ToString(),
            CreatedAt = recordset.CreatedAt,
            UpdatedBy = recordset.UpdatedBy?.ToString(),
            UpdatedAt = recordset.UpdatedAt
        };
    }

    public static RecordsetColumnDb ToDb(string recordsetId, Column column)
    {
        return new RecordsetColumnDb
        {
            RecordsetId = recordsetId,
            StorageKey = column.StorageKey ?? string.Empty,
            Name = column.Name,
            Type = column.Type,
            Required = column.Required,
            AllowedValuesJson = column.AllowedValues is null ? null : JsonSerializer.Serialize(column.AllowedValues),
            MinNumber = column.MinNumber,
            MaxNumber = column.MaxNumber,
            Regex = column.Regex
        };
    }

    public static RecordsetStatusDb ToDb(string recordsetId, Status status)
    {
        return new RecordsetStatusDb
        {
            RecordsetId = recordsetId,
            Name = status.Name,
            Color = status.Color
        };
    }

    public static RecordsetStatusTransitionDb ToDb(string recordsetId, StatusTransition transition)
    {
        return new RecordsetStatusTransitionDb
        {
            RecordsetId = recordsetId,
            From = transition.From,
            AllowedNextJson = JsonSerializer.Serialize(transition.AllowedNext)
        };
    }

    private static Column ToColumn(RecordsetColumnDb column)
    {
        var allowed = string.IsNullOrWhiteSpace(column.AllowedValuesJson)
            ? null
            : JsonSerializer.Deserialize<string[]>(column.AllowedValuesJson);

        return new Column
        {
            StorageKey = column.StorageKey,
            Name = column.Name,
            Type = column.Type,
            Required = column.Required,
            AllowedValues = allowed,
            MinNumber = column.MinNumber,
            MaxNumber = column.MaxNumber,
            Regex = column.Regex
        };
    }

    private static Status ToStatus(RecordsetStatusDb status)
    {
        return new Status
        {
            Name = status.Name,
            Color = status.Color
        };
    }

    private static StatusTransition ToTransition(RecordsetStatusTransitionDb transition)
    {
        var allowed = string.IsNullOrWhiteSpace(transition.AllowedNextJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(transition.AllowedNextJson) ?? Array.Empty<string>();

        return new StatusTransition
        {
            From = transition.From,
            AllowedNext = allowed
        };
    }
}