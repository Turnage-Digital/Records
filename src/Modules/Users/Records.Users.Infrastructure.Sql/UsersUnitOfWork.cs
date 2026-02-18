using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;
using Records.Users.Domain.Entities;
using Records.Users.Domain.Interfaces;
using Records.Users.Infrastructure.Sql.Entities;
using Records.Users.Infrastructure.Sql.Specifications;

namespace Records.Users.Infrastructure.Sql;

public sealed class UsersUnitOfWork(UsersDbContext dbContext) : IUsersUnitOfWork
{
    public Task<User?> GetUserByIdAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId.ToString(), cancellationToken);
    }

    public Task<User?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task AddRoleMembershipAsync(
        UlidId userId,
        UserRole role,
        UlidId? tenantId,
        UlidId grantedBy,
        DateTimeOffset grantedAt,
        CancellationToken cancellationToken
    )
    {
        dbContext.UserRoleMemberships.Add(new UserRoleMembershipDb
        {
            UserId = userId.ToString(),
            Role = role,
            TenantId = tenantId?.ToString(),
            GrantedBy = grantedBy.ToString(),
            GrantedAt = grantedAt
        });

        return Task.CompletedTask;
    }

    public async Task RemoveRoleMembershipAsync(
        UlidId userId,
        UserRole role,
        UlidId? tenantId,
        CancellationToken cancellationToken
    )
    {
        var userKey = userId.ToString();
        var tenantKey = tenantId?.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantSpec(userKey, role, tenantKey);
        var membership = await dbContext.UserRoleMemberships
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return;
        }

        dbContext.UserRoleMemberships.Remove(membership);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
