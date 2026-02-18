using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.Specifications;

namespace Records.Clocks.Infrastructure.Sql.Specifications;

public sealed class ClockDefinitionByTenantAndNameSpec : Specification<ClockDefinitionDb>
{
    public ClockDefinitionByTenantAndNameSpec(string tenantId, string name)
    {
        AddCriteria(d => d.TenantId == tenantId && d.Name == name);
    }
}
