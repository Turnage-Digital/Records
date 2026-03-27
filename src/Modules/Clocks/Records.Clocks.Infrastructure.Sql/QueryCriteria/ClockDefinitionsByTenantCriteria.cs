using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql.QueryCriteria;

public sealed class ClockDefinitionsByTenantCriteria : QueryCriteria<ClockDefinitionDb>
{
    public ClockDefinitionsByTenantCriteria(string tenantId)
    {
        AddCriteria(d => d.TenantId == tenantId);
        ApplyOrderBy(d => d.Name);
    }
}