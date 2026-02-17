using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts;
using Records.Users.Domain;

namespace Records.Users.Infrastructure.Sql.Queries;

public sealed class UserAccessQueries(UsersDbContext dbContext) : IUserAccessQueries
{
    public async Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.GlobalAdmin &&
                     x.TenantId == null,
                cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.TenantAdmin &&
                     x.TenantId == tenantKey,
                cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.TenantAdmin &&
                     x.TenantId != null,
                cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.Operations &&
                     x.TenantId != null,
                cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId.ToString();
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.Operations &&
                     x.TenantId == tenantKey,
                cancellationToken);
    }
}
