using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Domain.Entities;

public sealed class Record
{
    public Record(int id, UlidId recordsetId, object bag, UlidId createdBy, DateTimeOffset createdAt)
    {
        Id = id;
        RecordsetId = recordsetId;
        Bag = bag;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }
    public UlidId RecordsetId { get; private set; }
    public object Bag { get; private set; }
    public UlidId CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public UlidId? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void UpdateBag(object bag, UlidId updatedBy, DateTimeOffset updatedAt)
    {
        Bag = bag;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public void LoadUpdated(UlidId? updatedBy, DateTimeOffset? updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }
}