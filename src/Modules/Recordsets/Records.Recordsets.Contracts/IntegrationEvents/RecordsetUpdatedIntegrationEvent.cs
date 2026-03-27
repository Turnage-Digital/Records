using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordsetUpdatedIntegrationEvent : IIntegrationEvent
{
    public RecordsetUpdatedIntegrationEvent(
        UlidId recordsetId,
        UlidId updatedBy,
        DateTimeOffset updatedAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredAt = updatedAt;
        EventType = nameof(RecordsetUpdatedIntegrationEvent);
        RecordsetId = recordsetId;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public UlidId RecordsetId { get; }
    public UlidId UpdatedBy { get; }
    public DateTimeOffset UpdatedAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string EventType { get; }
}