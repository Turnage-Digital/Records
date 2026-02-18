using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetProjectionsOrderedByNameSpec : Specification<RecordsetProjectionDb>
{
    public RecordsetProjectionsOrderedByNameSpec()
    {
        ApplyOrderBy(x => x.Name);
    }
}
