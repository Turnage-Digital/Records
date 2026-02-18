using Records.Core.Domain.Specifications;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql.Specifications;

public sealed class RecordsetItemByRecordsetIdAndIdSpec : Specification<RecordDb>
{
    public RecordsetItemByRecordsetIdAndIdSpec(string recordsetId, int recordId)
    {
        AddCriteria(x => x.RecordsetId == recordsetId && x.Id == recordId);
    }
}
