using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Domain.Entities;

public class Tenant
{
    private Tenant()
    {
    }

    public Tenant(UlidId id, string name)
    {
        Id = id;
        Name = name;
        Status = TenantStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Tenant Rehydrate(UlidId id, string name, TenantStatus status, DateTimeOffset createdAt)
    {
        return new Tenant
        {
            Id = id,
            Name = name,
            Status = status,
            CreatedAt = createdAt
        };
    }

    public UlidId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Disable()
    {
        Status = TenantStatus.Disabled;
    }
}
