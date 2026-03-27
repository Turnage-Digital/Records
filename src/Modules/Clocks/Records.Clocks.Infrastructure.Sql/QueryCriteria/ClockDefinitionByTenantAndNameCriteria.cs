using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql.QueryCriteria;

public sealed class ClockDefinitionByTenantAndNameCriteria : QueryCriteria<ClockDefinitionDb>
{
    public ClockDefinitionByTenantAndNameCriteria(string tenantId, string name)
    {
        AddCriteria(d => d.TenantId == tenantId && d.Name == name);
    }
}