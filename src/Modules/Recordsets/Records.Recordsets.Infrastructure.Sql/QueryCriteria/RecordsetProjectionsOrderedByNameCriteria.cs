using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.QueryCriteria;

public sealed class RecordsetProjectionsOrderedByNameCriteria : QueryCriteria<RecordsetProjectionDb>
{
    public RecordsetProjectionsOrderedByNameCriteria()
    {
        ApplyOrderBy(x => x.Name);
    }
}