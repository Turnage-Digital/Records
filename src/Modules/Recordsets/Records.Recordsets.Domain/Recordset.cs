using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Events;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Domain;

public sealed class Recordset : AggregateRoot
{
    private readonly List<Column> _columns = [];
    private readonly List<Status> _statuses = [];
    private readonly List<StatusTransition> _transitions = [];

    private Recordset()
    {
    }

    public Recordset(UlidId id, string name)
    {
        Id = id;
        Name = name;
    }

    public UlidId Id { get; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<Column> Columns => _columns;
    public IReadOnlyList<Status> Statuses => _statuses;
    public IReadOnlyList<StatusTransition> StatusTransitions => _transitions;

    public static Recordset Create(
        UlidId id,
        string name,
        IReadOnlyList<Column> initialColumns,
        IReadOnlyList<Status> initialStatuses,
        IReadOnlyList<StatusTransition> initialTransitions,
        DateTimeOffset occurredAt
    )
    {
        var recordset = new Recordset(id, name);
        recordset._columns.AddRange(AssignStorageKeys(initialColumns));
        recordset._statuses.AddRange(initialStatuses);
        recordset._transitions.AddRange(initialTransitions);
        recordset.AddDomainEvent(new RecordsetCreated(id, name, occurredAt));
        return recordset;
    }

    public void UpdateSchema(
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> nextStatuses,
        IReadOnlyList<StatusTransition> nextTransitions,
        DateTimeOffset occurredAt
    )
    {
        ApplySchema(nextColumns, nextStatuses, nextTransitions, true);
        AddDomainEvent(new RecordsetUpdated(Id, occurredAt));
    }

    public void Rename(string name, DateTimeOffset occurredAt)
    {
        Name = name;
        AddDomainEvent(new RecordsetUpdated(Id, occurredAt));
    }

    public void LoadSchema(
        IReadOnlyList<Column> nextColumns,
        IReadOnlyList<Status> nextStatuses,
        IReadOnlyList<StatusTransition> nextTransitions
    )
    {
        ApplySchema(nextColumns, nextStatuses, nextTransitions, false);
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
        bool assignStorageKeys
    )
    {
        _columns.Clear();
        _columns.AddRange(assignStorageKeys ? AssignStorageKeys(nextColumns) : nextColumns);
        _statuses.Clear();
        _statuses.AddRange(nextStatuses);
        _transitions.Clear();
        _transitions.AddRange(nextTransitions);
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
