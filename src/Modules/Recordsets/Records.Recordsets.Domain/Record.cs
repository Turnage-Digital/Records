using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Domain;

public sealed class Record
{
    public Record(int id, UlidId recordsetId, object bag)
    {
        Id = id;
        RecordsetId = recordsetId;
        Bag = bag;
    }

    public int Id { get; internal set; }
    public UlidId RecordsetId { get; private set; }
    public object Bag { get; private set; }

    public void UpdateBag(object bag)
    {
        Bag = bag;
    }
}
