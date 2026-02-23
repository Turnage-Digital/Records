using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Contracts.Queries;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.QueryCriteria;

namespace Records.Users.Infrastructure.Sql;

public sealed class UserAccessQueries(UsersDbContext dbContext) : IUserAccessQueries
{
    public async Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantCriteria(userKey, UserRole.GlobalAdmin, null);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplyCriteria(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var tenantKey = tenantId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantCriteria(userKey, UserRole.TenantAdmin, tenantKey);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplyCriteria(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleWithAnyTenantCriteria(userKey, UserRole.TenantAdmin);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplyCriteria(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleWithAnyTenantCriteria(userKey, UserRole.Operations);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplyCriteria(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var tenantKey = tenantId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantCriteria(userKey, UserRole.Operations, tenantKey);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplyCriteria(spec)
            .AnyAsync(cancellationToken);
    }
}