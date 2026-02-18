using Records.Core.Domain.Specifications;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.Specifications;

public sealed class UserRoleMembershipByUserAndRoleWithAnyTenantSpec : Specification<UserRoleMembershipDb>
{
    public UserRoleMembershipByUserAndRoleWithAnyTenantSpec(string userId, UserRole role)
    {
        AddCriteria(x => x.UserId == userId && x.Role == role && x.TenantId != null);
    }
}
