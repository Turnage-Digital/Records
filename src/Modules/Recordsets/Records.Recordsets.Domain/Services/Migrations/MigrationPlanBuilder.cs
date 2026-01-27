using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Services.Migrations;

public static class MigrationPlanBuilder
{
    public static MigrationPlan Build(
        IReadOnlyList<Column> currentColumns,
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> currentStatuses,
        IReadOnlyList<Status> nextStatuses
    )
    {
        var changeColumnTypes = new Dictionary<string, ChangeColumnTypeOp>(StringComparer.OrdinalIgnoreCase);
        var currentByKey = currentColumns
            .Where(c => !string.IsNullOrWhiteSpace(c.StorageKey))
            .ToDictionary(c => c.StorageKey!, StringComparer.OrdinalIgnoreCase);

        foreach (var next in nextColumns)
        {
            if (string.IsNullOrWhiteSpace(next.StorageKey))
            {
                continue;
            }

            if (currentByKey.TryGetValue(next.StorageKey!, out var current))
            {
                if (current.Type != next.Type)
                {
                    changeColumnTypes[next.StorageKey!] = new ChangeColumnTypeOp(
                        next.StorageKey!,
                        next.Type,
                        "column type changed"
                    );
                }
            }
        }

        var nextStatusNames = nextStatuses.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removeStatuses = currentStatuses
            .Where(s => !nextStatusNames.Contains(s.Name))
            .Select(s => new RemoveStatusOp(s.Name, null))
            .ToArray();

        return new MigrationPlan
        {
            ChangeColumnTypes = changeColumnTypes.Count > 0 ? changeColumnTypes.Values.ToArray() : null,
            RemoveStatuses = removeStatuses.Length > 0 ? removeStatuses : null
        };
    }
}