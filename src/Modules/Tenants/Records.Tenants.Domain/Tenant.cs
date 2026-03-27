using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain.Events;

namespace Records.Tenants.Domain;

public sealed class Tenant : AggregateRoot
{
    private Tenant()
    {
    }

    private Tenant(UlidId id, string name)
    {
        Id = id;
        Name = name;
        Status = TenantStatus.Active;
    }

    public UlidId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }

    public static Tenant Create(UlidId id, string name, DateTimeOffset occurredAt)
    {
        var tenant = new Tenant(id, name);
        tenant.AddDomainEvent(new TenantCreated(id, name, tenant.Status, occurredAt));
        return tenant;
    }

    public static Tenant Rehydrate(UlidId id, string name, TenantStatus status)
    {
        return new Tenant
        {
            Id = id,
            Name = name,
            Status = status
        };
    }

    public void Disable(DateTimeOffset occurredAt)
    {
        if (Status == TenantStatus.Disabled)
        {
            return;
        }

        Status = TenantStatus.Disabled;
        AddDomainEvent(new TenantDisabled(Id));
    }

    public override string GetStreamId()
    {
        return $"Tenant:{Id}";
    }

    public override string GetStreamType()
    {
        return "Tenant";
    }
}