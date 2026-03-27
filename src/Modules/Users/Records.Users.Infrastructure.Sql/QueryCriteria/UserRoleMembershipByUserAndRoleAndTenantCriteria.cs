using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.QueryCriteria;

public sealed class UserRoleMembershipByUserAndRoleAndTenantCriteria : QueryCriteria<UserRoleMembershipDb>
{
    public UserRoleMembershipByUserAndRoleAndTenantCriteria(string userId, UserRole role, string? tenantId)
    {
        AddCriteria(x => x.UserId == userId && x.Role == role && x.TenantId == tenantId);
    }
}