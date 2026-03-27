using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql.QueryCriteria;

public sealed class ClocksByRecordCriteria : QueryCriteria<ClockDb>
{
    public ClocksByRecordCriteria(string recordsetId, int recordId)
    {
        AddCriteria(c => c.RecordsetId == recordsetId && c.RecordId == recordId);
        ApplyOrderBy(c => c.StartedAt);
    }
}