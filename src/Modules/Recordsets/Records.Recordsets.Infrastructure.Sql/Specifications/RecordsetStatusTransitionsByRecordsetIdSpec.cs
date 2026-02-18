using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetStatusTransitionsByRecordsetIdSpec : Specification<RecordsetStatusTransitionDb>
{
    public RecordsetStatusTransitionsByRecordsetIdSpec(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}
