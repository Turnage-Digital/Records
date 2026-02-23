namespace Records.Recordsets.Domain;

public sealed record MigrationPlan
{
    public ChangeColumnTypeOp[]? ChangeColumnTypes { get; init; }
    public RemoveStatusOp[]? RemoveStatuses { get; init; }
}

public sealed record ChangeColumnTypeOp(
    string StorageKey,
    ColumnType TargetType,
    string Reason
);

public sealed record RemoveStatusOp(
    string Name,
    string? Replacement
);