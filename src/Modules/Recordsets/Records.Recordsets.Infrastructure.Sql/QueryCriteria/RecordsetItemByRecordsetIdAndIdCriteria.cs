using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetItemByRecordsetIdAndIdCriteria : QueryCriteria<RecordDb>
{
    public RecordsetItemByRecordsetIdAndIdCriteria(string recordsetId, int recordId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId && x.Id == recordId);
    }
}