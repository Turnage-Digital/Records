using Records.Users.Domain;

namespace Records.Users.Infrastructure.Sql.Entities;

public class UserRoleMembershipDb
{
    public long Id { get; set; }

    public string UserId { get; set; } = null!;

    public string? TenantId { get; set; }

    public UserRole Role { get; set; }

    public string GrantedBy { get; set; } = string.Empty;

    public DateTimeOffset GrantedAt { get; set; }
}