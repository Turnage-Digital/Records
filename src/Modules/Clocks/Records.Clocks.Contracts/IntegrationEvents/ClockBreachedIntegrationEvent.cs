using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.IntegrationEvents;

public sealed class ClockBreachedIntegrationEvent : IIntegrationEvent
{
    public ClockBreachedIntegrationEvent(
        UlidId clockId,
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        string clockName,
        DateTimeOffset breachedAt,
        DateTimeOffset breachDueAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = breachedAt;
        EventType = nameof(ClockBreachedIntegrationEvent);
        ClockId = clockId;
        TenantId = tenantId;
        RecordsetId = recordsetId;
        RecordId = recordId;
        DefinitionId = definitionId;
        ClockName = clockName;
        BreachedAt = breachedAt;
        BreachDueAt = breachDueAt;
    }

    public UlidId ClockId { get; }
    public UlidId TenantId { get; }
    public UlidId RecordsetId { get; }
    public int RecordId { get; }
    public UlidId DefinitionId { get; }
    public string ClockName { get; }
    public DateTimeOffset BreachedAt { get; }
    public DateTimeOffset BreachDueAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}
