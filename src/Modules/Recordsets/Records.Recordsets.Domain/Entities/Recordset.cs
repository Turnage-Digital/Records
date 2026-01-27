using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Events;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Entities;

public sealed class Recordset : AggregateRoot
{
    private readonly List<Column> columns = [];
    private readonly List<Status> statuses = [];
    private readonly List<StatusTransition> transitions = [];

    private Recordset()
    {
    }

    public Recordset(UlidId id, string name, UlidId createdBy, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public UlidId Id { get; }
    public string Name { get; private set; } = string.Empty;
    public UlidId CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
    public UlidId? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyList<Column> Columns => columns;
    public IReadOnlyList<Status> Statuses => statuses;
    public IReadOnlyList<StatusTransition> StatusTransitions => transitions;

    public static Recordset Create(
        UlidId id,
        string name,
        UlidId createdBy,
        DateTimeOffset createdAt,
        IReadOnlyList<Column> initialColumns,
        IReadOnlyList<Status> initialStatuses,
        IReadOnlyList<StatusTransition> initialTransitions
    )
    {
        var recordset = new Recordset(id, name, createdBy, createdAt);
        recordset.columns.AddRange(AssignStorageKeys(initialColumns));
        recordset.statuses.AddRange(initialStatuses);
        recordset.transitions.AddRange(initialTransitions);
        recordset.AddDomainEvent(new RecordsetCreated(id, name, createdBy, createdAt));
        return recordset;
    }

    public void UpdateSchema(
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> nextStatuses,
        IReadOnlyList<StatusTransition> nextTransitions,
        UlidId updatedBy,
        DateTimeOffset updatedAt
    )
    {
        ApplySchema(nextColumns, nextStatuses, nextTransitions, updatedBy, updatedAt, true);
        AddDomainEvent(new RecordsetUpdated(Id, updatedBy, updatedAt));
    }

    public void Rename(string name, UlidId updatedBy, DateTimeOffset updatedAt)
    {
        Name = name;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        AddDomainEvent(new RecordsetUpdated(Id, updatedBy, updatedAt));
    }

    public void LoadSchema(
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> nextStatuses,
        IReadOnlyList<StatusTransition> nextTransitions,
        UlidId? updatedBy,
        DateTimeOffset? updatedAt
    )
    {
        ApplySchema(nextColumns, nextStatuses, nextTransitions, updatedBy ?? CreatedBy, updatedAt ?? CreatedAt,
            false);
        ClearDomainEvents();
    }

    public override string GetStreamId()
    {
        return $"Recordset:{Id}";
    }

    public override string GetStreamType()
    {
        return "Recordset";
    }

    private void ApplySchema(
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> nextStatuses,
        IReadOnlyList<StatusTransition> nextTransitions,
        UlidId updatedBy,
        DateTimeOffset updatedAt,
        bool assignStorageKeys
    )
    {
        columns.Clear();
        columns.AddRange(assignStorageKeys ? AssignStorageKeys(nextColumns) : nextColumns);
        statuses.Clear();
        statuses.AddRange(nextStatuses);
        transitions.Clear();
        transitions.AddRange(nextTransitions);

        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    private static List<Column> AssignStorageKeys(IEnumerable<Column> incoming)
    {
        var normalized = incoming
            .Select(column => column with { StorageKey = column.StorageKey })
            .ToList();

        var used = new HashSet<string>(
            normalized
                .Where(c => !string.IsNullOrWhiteSpace(c.StorageKey))
                .Select(c => c.StorageKey!)
                .Select(key => key.ToLowerInvariant()));

        var counter = 1;
        for (var i = 0; i < normalized.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(normalized[i].StorageKey))
            {
                continue;
            }

            string key;
            do
            {
                key = $"prop{counter++}";
            } while (used.Contains(key));

            normalized[i] = normalized[i].WithStorageKey(key);
            used.Add(key);
        }

        return normalized;
    }
}