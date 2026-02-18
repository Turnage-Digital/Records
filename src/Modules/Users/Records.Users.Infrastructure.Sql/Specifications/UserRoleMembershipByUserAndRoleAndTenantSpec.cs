using Records.Core.Domain.Specifications;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.Specifications;

public sealed class UserRoleMembershipByUserAndRoleAndTenantSpec : Specification<UserRoleMembershipDb>
{
    public UserRoleMembershipByUserAndRoleAndTenantSpec(string userId, UserRole role, string? tenantId)
    {
        AddCriteria(x => x.UserId == userId && x.Role == role && x.TenantId == tenantId);
    }
}
