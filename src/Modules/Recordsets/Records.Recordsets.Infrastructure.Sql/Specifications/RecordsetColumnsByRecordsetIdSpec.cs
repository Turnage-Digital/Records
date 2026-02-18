using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetColumnsByRecordsetIdSpec : Specification<RecordsetColumnDb>
{
    public RecordsetColumnsByRecordsetIdSpec(string recordsetId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId);
    }
}
