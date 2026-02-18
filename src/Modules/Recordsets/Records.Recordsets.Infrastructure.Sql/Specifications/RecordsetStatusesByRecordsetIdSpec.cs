using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetStatusesByRecordsetIdSpec : Specification<RecordsetStatusDb>
{
    public RecordsetStatusesByRecordsetIdSpec(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}
