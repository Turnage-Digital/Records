using Records.Core.Domain.Specifications;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.Specifications;

public sealed class UserProjectionByUserIdSpec : Specification<UserProjectionDb>
{
    public UserProjectionByUserIdSpec(string userId)
    {
        AddCriteria(x => x.UserId == userId);
    }
}
