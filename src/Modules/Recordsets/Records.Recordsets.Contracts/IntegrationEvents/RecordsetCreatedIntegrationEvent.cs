using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordsetCreatedIntegrationEvent : IIntegrationEvent
{
    public RecordsetCreatedIntegrationEvent(
        UlidId recordsetId,
        UlidId createdBy,
        DateTimeOffset createdAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = createdAt;
        EventType = nameof(RecordsetCreatedIntegrationEvent);
        RecordsetId = recordsetId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public UlidId RecordsetId { get; }
    public UlidId CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}