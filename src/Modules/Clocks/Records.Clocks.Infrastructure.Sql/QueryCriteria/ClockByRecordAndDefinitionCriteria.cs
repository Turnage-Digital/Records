using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql.QueryCriteria;

public sealed class ClockByRecordAndDefinitionCriteria : QueryCriteria<ClockDb>
{
    public ClockByRecordAndDefinitionCriteria(string recordsetId, int recordId, string definitionId)
    {
        AddCriteria(c =>
            c.RecordsetId == recordsetId &&
            c.RecordId == recordId &&
            c.DefinitionId == definitionId);
    }
}