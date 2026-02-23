using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetStatusesByRecordsetIdCriteria : QueryCriteria<RecordsetStatusDb>
{
    public RecordsetStatusesByRecordsetIdCriteria(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}