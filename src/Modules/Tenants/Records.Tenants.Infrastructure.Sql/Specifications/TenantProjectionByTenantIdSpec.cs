using Records.Core.Domain.Specifications;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql.Specifications;

public sealed class TenantProjectionByTenantIdSpec : Specification<TenantProjectionDb>
{
    public TenantProjectionByTenantIdSpec(string tenantId)
    {
        AddCriteria(x => x.TenantId == tenantId);
    }
}
