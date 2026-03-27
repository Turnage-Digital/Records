using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.QueryCriteria;

public sealed class UserRoleMembershipByUserAndRoleWithAnyTenantCriteria : QueryCriteria<UserRoleMembershipDb>
{
    public UserRoleMembershipByUserAndRoleWithAnyTenantCriteria(string userId, UserRole role)
    {
        AddCriteria(x => x.UserId == userId && x.Role == role && x.TenantId != null);
    }
}