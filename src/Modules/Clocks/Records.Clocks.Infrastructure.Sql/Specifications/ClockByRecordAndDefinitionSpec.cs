using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.Specifications;

namespace Records.Clocks.Infrastructure.Sql.Specifications;

public sealed class ClockByRecordAndDefinitionSpec : Specification<ClockDb>
{
    public ClockByRecordAndDefinitionSpec(string recordsetId, int recordId, string definitionId)
    {
        AddCriteria(c =>
            c.RecordsetId == recordsetId &&
            c.RecordId == recordId &&
            c.DefinitionId == definitionId);
    }
}
