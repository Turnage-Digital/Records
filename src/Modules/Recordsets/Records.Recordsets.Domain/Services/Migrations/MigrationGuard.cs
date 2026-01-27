using Records.Recordsets.Domain.Exceptions;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Services.Migrations;

public static class MigrationGuard
{
    public static void ThrowIfRequired(
        IReadOnlyList<Column> currentColumns,
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> currentStatuses,
        IReadOnlyList<Status> nextStatuses
    )
    {
        var plan = MigrationPlanBuilder.Build(currentColumns, nextColumns, currentStatuses, nextStatuses);

        var hasColumnChanges = plan.ChangeColumnTypes is { Length: > 0 };
        var hasStatusRemovals = plan.RemoveStatuses is { Length: > 0 };

        if (!hasColumnChanges && !hasStatusRemovals)
        {
            return;
        }

        throw new MigrationRequiredException("Schema changes require a migration plan.");
    }
}