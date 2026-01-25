using Records.Tenants.Domain;

namespace Records.Tenants.Infrastructure.Sql.Entities;

public class TenantProjectionDb
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}