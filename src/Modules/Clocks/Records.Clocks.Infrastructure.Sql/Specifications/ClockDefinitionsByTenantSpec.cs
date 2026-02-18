using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.Specifications;

namespace Records.Clocks.Infrastructure.Sql.Specifications;

public sealed class ClockDefinitionsByTenantSpec : Specification<ClockDefinitionDb>
{
    public ClockDefinitionsByTenantSpec(string tenantId)
    {
        AddCriteria(d => d.TenantId == tenantId);
        ApplyOrderBy(d => d.Name);
    }
}
