using Records.Users.Domain;

namespace Records.Users.Infrastructure.Sql.Entities;

public class UserProjectionDb
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public UserStatus Status { get; set; }
}