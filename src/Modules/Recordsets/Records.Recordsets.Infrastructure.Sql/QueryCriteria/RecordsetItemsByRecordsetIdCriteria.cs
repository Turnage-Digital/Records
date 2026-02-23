using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetItemsByRecordsetIdCriteria : QueryCriteria<RecordDb>
{
    public RecordsetItemsByRecordsetIdCriteria(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}