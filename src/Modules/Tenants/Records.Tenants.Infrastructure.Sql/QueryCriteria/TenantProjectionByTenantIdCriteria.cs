using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql.QueryCriteria;

public sealed class TenantProjectionByTenantIdCriteria : QueryCriteria<TenantProjectionDb>
{
    public TenantProjectionByTenantIdCriteria(string tenantId)
    {
        AddCriteria(x => x.TenantId == tenantId);
    }
}