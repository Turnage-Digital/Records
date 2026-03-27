using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetByIdCriteria : QueryCriteria<RecordsetDb>
{
    public RecordsetByIdCriteria(string recordsetId)
    {
        AddCriteria(x => x.Id == recordsetId);
    }
}