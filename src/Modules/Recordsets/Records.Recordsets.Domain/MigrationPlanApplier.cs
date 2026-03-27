using System.Text.Json;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain;

public static class MigrationPlanApplier
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static MigrationPlanContext Prepare(
        MigrationPlan plan,
        IReadOnlyList<Column> existingColumns,
        IReadOnlyList<Status> existingStatuses,
        IReadOnlyList<StatusTransition> existingTransitions
    )
    {
        var columns = existingColumns.Select(CloneColumn).ToList();
        var statuses = existingStatuses.Select(CloneStatus).ToList();
        var transitions = existingTransitions.Select(CloneTransition).ToList();

        var columnTypeChanges = new List<ColumnTypeChange>();
        var statusMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (plan.ChangeColumnTypes is { Length: > 0 })
        {
            foreach (var op in plan.ChangeColumnTypes)
            {
                var column = columns.FirstOrDefault(c =>
                    string.Equals(KeyOf(c), op.StorageKey, StringComparison.OrdinalIgnoreCase));
                if (column == null)
                {
                    continue;
                }

                column = column with { Type = op.TargetType };
                var key = column.StorageKey ?? column.Property;
                columnTypeChanges.Add(new ColumnTypeChange(key, op.TargetType));
            }
        }

        if (plan.RemoveStatuses is { Length: > 0 })
        {
            var removed = plan.RemoveStatuses
                .Select(s => s.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var op in plan.RemoveStatuses)
            {
                if (!string.IsNullOrWhiteSpace(op.Replacement))
                {
                    statusMapping[op.Name] = op.Replacement!;
                }
            }

            statuses = statuses
                .Where(s => !removed.Contains(s.Name))
                .ToList();

            transitions = transitions
                .Where(t => !removed.Contains(t.From))
                .Select(t => new StatusTransition
                {
                    From = t.From,
                    AllowedNext = t.AllowedNext
                        .Select(next => statusMapping.TryGetValue(next, out var mapped) ? mapped : next)
                        .Where(next => !removed.Contains(next))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                })
                .Where(t => statuses.Any(s =>
                    string.Equals(s.Name, t.From, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return new MigrationPlanContext(
            columns,
            statuses,
            transitions,
            columnTypeChanges,
            statusMapping
        );

        string KeyOf(Column c)
        {
            return string.IsNullOrWhiteSpace(c.StorageKey) ? c.Property : c.StorageKey!;
        }
    }

    public static Dictionary<string, object?> ApplyToRecord(MigrationPlanContext context, object bag)
    {
        var dict = ToDictionary(bag);
        ApplyColumnTypeChanges(context, dict);
        ApplyStatusMapping(context, dict);
        return dict;
    }

    private static void ApplyColumnTypeChanges(MigrationPlanContext context, IDictionary<string, object?> bag)
    {
        foreach (var change in context.ColumnTypeChanges)
        {
            if (!bag.TryGetValue(change.Key, out var value))
            {
                continue;
            }

            bag[change.Key] = ConvertValue(value, change.TargetType);
        }
    }

    private static void ApplyStatusMapping(MigrationPlanContext context, IDictionary<string, object?> bag)
    {
        if (context.StatusMapping.Count == 0)
        {
            return;
        }

        if (!bag.TryGetValue("status", out var current) || current is null)
        {
            return;
        }

        var status = current.ToString();
        if (status is null)
        {
            return;
        }

        if (context.StatusMapping.TryGetValue(status, out var mapped))
        {
            bag["status"] = mapped;
        }
    }

    private static Dictionary<string, object?> ToDictionary(object bag)
    {
        if (bag is Dictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        var json = JsonSerializer.Serialize(bag, SerializerOptions);
        var fallback = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, SerializerOptions) ??
                       new Dictionary<string, object?>();
        return new Dictionary<string, object?>(fallback, StringComparer.OrdinalIgnoreCase);
    }

    private static object? ConvertValue(object? value, ColumnType targetType)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return targetType switch
            {
                ColumnType.Text => value.ToString(),
                ColumnType.Number => TryToDecimal(value),
                ColumnType.Boolean => TryToBool(value),
                ColumnType.Date => TryToDate(value),
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }

    private static object? TryToDecimal(object value)
    {
        if (value is decimal or int or long or double or float)
        {
            return Convert.ToDecimal(value);
        }

        if (value is string s && decimal.TryParse(s, out var result))
        {
            return result;
        }

        return value;
    }

    private static object? TryToBool(object value)
    {
        if (value is bool b)
        {
            return b;
        }

        if (value is string s && bool.TryParse(s, out var result))
        {
            return result;
        }

        return value;
    }

    private static object? TryToDate(object value)
    {
        if (value is DateTime dt)
        {
            return dt;
        }

        if (value is string s && DateTime.TryParse(s, out var result))
        {
            return result;
        }

        return value;
    }

    private static Column CloneColumn(Column column)
    {
        return new Column
        {
            StorageKey = column.StorageKey,
            Name = column.Name,
            Type = column.Type,
            Required = column.Required,
            AllowedValues = column.AllowedValues,
            MinNumber = column.MinNumber,
            MaxNumber = column.MaxNumber,
            Regex = column.Regex
        };
    }

    private static Status CloneStatus(Status status)
    {
        return new Status
        {
            Name = status.Name,
            Color = status.Color
        };
    }

    private static StatusTransition CloneTransition(StatusTransition transition)
    {
        return new StatusTransition
        {
            From = transition.From,
            AllowedNext = transition.AllowedNext.ToArray()
        };
    }
}

public sealed record MigrationPlanContext(
    IReadOnlyList<Column> Columns,
    IReadOnlyList<Status> Statuses,
    IReadOnlyList<StatusTransition> StatusTransitions,
    IReadOnlyList<ColumnTypeChange> ColumnTypeChanges,
    IReadOnlyDictionary<string, string> StatusMapping
);

public sealed record ColumnTypeChange(string Key, ColumnType TargetType);