using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordUpdatedIntegrationEvent : IIntegrationEvent
{
    public RecordUpdatedIntegrationEvent(
        UlidId recordsetId,
        int? recordId,
        UlidId updatedBy,
        DateTimeOffset updatedAt,
        object? previousBag,
        object newBag
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = updatedAt;
        EventType = nameof(RecordUpdatedIntegrationEvent);
        RecordsetId = recordsetId;
        RecordId = recordId;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        PreviousBag = previousBag;
        NewBag = newBag;
    }

    public UlidId RecordsetId { get; }
    public int? RecordId { get; }
    public UlidId UpdatedBy { get; }
    public DateTimeOffset UpdatedAt { get; }
    public object? PreviousBag { get; }
    public object NewBag { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}