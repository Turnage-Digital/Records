using Microsoft.AspNetCore.Identity;

namespace Records.Users.Domain.Entities;

public class User : IdentityUser
{
    public string? DisplayName { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Invited;
}