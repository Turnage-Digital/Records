using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetByIdSpec : Specification<RecordsetDb>
{
    public RecordsetByIdSpec(string recordsetId)
    {
        AddCriteria(x => x.Id == recordsetId);
    }
}
