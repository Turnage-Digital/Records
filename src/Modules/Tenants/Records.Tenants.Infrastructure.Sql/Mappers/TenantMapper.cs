using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql.Mappers;

public static class TenantMapper
{
    public static TenantDb ToDb(Tenant tenant)
    {
        return new TenantDb
        {
            Id = tenant.Id.ToString(),
            Name = tenant.Name,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }

    public static Tenant ToDomain(TenantDb entity)
    {
        return Tenant.Rehydrate(
            UlidId.Parse(entity.Id),
            entity.Name,
            entity.Status,
            entity.CreatedAt);
    }

    public static void UpdateDb(Tenant domain, TenantDb entity)
    {
        entity.Name = domain.Name;
        entity.Status = domain.Status;
        entity.CreatedAt = domain.CreatedAt;
    }
}