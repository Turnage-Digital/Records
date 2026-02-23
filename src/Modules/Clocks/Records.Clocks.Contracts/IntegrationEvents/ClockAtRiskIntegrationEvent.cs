using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.IntegrationEvents;

public sealed class ClockAtRiskIntegrationEvent : IIntegrationEvent
{
    public ClockAtRiskIntegrationEvent(
        UlidId clockId,
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        string clockName,
        DateTimeOffset atRiskAt,
        DateTimeOffset breachDueAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = atRiskAt;
        EventType = nameof(ClockAtRiskIntegrationEvent);
        ClockId = clockId;
        TenantId = tenantId;
        RecordsetId = recordsetId;
        RecordId = recordId;
        DefinitionId = definitionId;
        ClockName = clockName;
        AtRiskAt = atRiskAt;
        BreachDueAt = breachDueAt;
    }

    public UlidId ClockId { get; }
    public UlidId TenantId { get; }
    public UlidId RecordsetId { get; }
    public int RecordId { get; }
    public UlidId DefinitionId { get; }
    public string ClockName { get; }
    public DateTimeOffset AtRiskAt { get; }
    public DateTimeOffset BreachDueAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}