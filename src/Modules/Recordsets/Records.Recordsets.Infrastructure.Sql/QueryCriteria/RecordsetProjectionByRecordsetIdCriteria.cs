using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetProjectionByRecordsetIdCriteria : QueryCriteria<RecordsetProjectionDb>
{
    public RecordsetProjectionByRecordsetIdCriteria(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}