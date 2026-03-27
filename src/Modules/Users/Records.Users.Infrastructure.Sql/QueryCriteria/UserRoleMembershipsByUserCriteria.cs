using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.QueryCriteria;

public sealed class UserRoleMembershipsByUserCriteria : QueryCriteria<UserRoleMembershipDb>
{
    public UserRoleMembershipsByUserCriteria(string userId)
    {
        AddCriteria(x => x.UserId == userId);
        ApplyOrderByDescending(x => x.GrantedAt);
    }
}