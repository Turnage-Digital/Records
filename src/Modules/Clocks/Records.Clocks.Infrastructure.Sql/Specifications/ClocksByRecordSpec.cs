using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.Specifications;

namespace Records.Clocks.Infrastructure.Sql.Specifications;

public sealed class ClocksByRecordSpec : Specification<ClockDb>
{
    public ClocksByRecordSpec(string recordsetId, int recordId)
    {
        AddCriteria(c => c.RecordsetId == recordsetId && c.RecordId == recordId);
        ApplyOrderBy(c => c.StartedAt);
    }
}
