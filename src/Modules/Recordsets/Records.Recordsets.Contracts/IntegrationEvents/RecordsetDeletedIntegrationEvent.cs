using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordsetDeletedIntegrationEvent : IIntegrationEvent
{
    public RecordsetDeletedIntegrationEvent(
        UlidId recordsetId,
        UlidId deletedBy,
        DateTimeOffset deletedAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredOn = deletedAt;
        EventType = nameof(RecordsetDeletedIntegrationEvent);
        RecordsetId = recordsetId;
        DeletedBy = deletedBy;
        DeletedAt = deletedAt;
    }

    public UlidId RecordsetId { get; }
    public UlidId DeletedBy { get; }
    public DateTimeOffset DeletedAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
}