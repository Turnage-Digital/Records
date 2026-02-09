using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordCreatedIntegrationEvent : IIntegrationEvent
{
    public RecordCreatedIntegrationEvent(
        UlidId recordsetId,
        int? recordId,
        UlidId createdBy,
        DateTimeOffset createdAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = createdAt;
        EventType = nameof(RecordCreatedIntegrationEvent);
        RecordsetId = recordsetId;
        RecordId = recordId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public UlidId RecordsetId { get; }
    public int? RecordId { get; }
    public UlidId CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}
