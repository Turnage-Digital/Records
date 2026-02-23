using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.QueryCriteria;

public sealed class UserProjectionByUserIdCriteria : QueryCriteria<UserProjectionDb>
{
    public UserProjectionByUserIdCriteria(string userId)
    {
        AddCriteria(x => x.UserId == userId);
    }
}