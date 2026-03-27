using Records.Core.Contracts.IntegrationEvents;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.IntegrationEvents;

public sealed class RecordDeletedIntegrationEvent : IIntegrationEvent
{
    public RecordDeletedIntegrationEvent(
        UlidId recordsetId,
        int? recordId,
        UlidId deletedBy,
        DateTimeOffset deletedAt
    )
    {
        EventId = Guid.NewGuid();
        OccurredAt = deletedAt;
        EventType = nameof(RecordDeletedIntegrationEvent);
        RecordsetId = recordsetId;
        RecordId = recordId;
        DeletedBy = deletedBy;
        DeletedAt = deletedAt;
    }

    public UlidId RecordsetId { get; }
    public int? RecordId { get; }
    public UlidId DeletedBy { get; }
    public DateTimeOffset DeletedAt { get; }

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string EventType { get; }
}