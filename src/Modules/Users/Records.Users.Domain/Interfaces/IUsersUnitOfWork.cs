using Records.Core.Domain.ValueObjects;
using Records.Users.Domain.Entities;

namespace Records.Users.Domain.Interfaces;

public interface IUsersUnitOfWork
{
    Task<User?> GetUserByIdAsync(UlidId userId, CancellationToken cancellationToken);
    Task<User?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddRoleMembershipAsync(
        UlidId userId,
        UserRole role,
        UlidId? tenantId,
        UlidId grantedBy,
        DateTimeOffset grantedAt,
        CancellationToken cancellationToken
    );

    Task RemoveRoleMembershipAsync(UlidId userId, UserRole role, UlidId? tenantId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}