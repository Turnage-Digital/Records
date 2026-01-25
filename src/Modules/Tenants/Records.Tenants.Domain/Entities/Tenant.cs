namespace Records.Tenants.Domain.Entities;

public class Tenant
{
    private Tenant()
    {
    }

    public Tenant(Guid id, string name)
    {
        Id = id;
        Name = name;
        Status = TenantStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Disable()
    {
        Status = TenantStatus.Disabled;
    }
}
