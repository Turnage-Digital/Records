using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Specifications;

namespace Records.Users.Infrastructure.Sql;

public sealed class UserAccessQueries(UsersDbContext dbContext) : IUserAccessQueries
{
    public async Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantSpec(userKey, UserRole.GlobalAdmin, null);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var tenantKey = tenantId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantSpec(userKey, UserRole.TenantAdmin, tenantKey);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsTenantAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleWithAnyTenantSpec(userKey, UserRole.TenantAdmin);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleWithAnyTenantSpec(userKey, UserRole.Operations);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(spec)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> IsOperationsAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var tenantKey = tenantId.ToString();
        var spec = new UserRoleMembershipByUserAndRoleAndTenantSpec(userKey, UserRole.Operations, tenantKey);
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(spec)
            .AnyAsync(cancellationToken);
    }
}
