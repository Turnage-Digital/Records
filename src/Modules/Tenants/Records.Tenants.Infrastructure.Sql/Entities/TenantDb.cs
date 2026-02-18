using Records.Tenants.Domain;

namespace Records.Tenants.Infrastructure.Sql.Entities;

public class TenantDb
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
