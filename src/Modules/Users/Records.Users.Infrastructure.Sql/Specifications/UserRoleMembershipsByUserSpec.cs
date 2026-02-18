using Records.Core.Domain.Specifications;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.Specifications;

public sealed class UserRoleMembershipsByUserSpec : Specification<UserRoleMembershipDb>
{
    public UserRoleMembershipsByUserSpec(string userId)
    {
        AddCriteria(x => x.UserId == userId);
        ApplyOrderByDescending(x => x.GrantedAt);
    }
}
