using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;
using Records.Users.Infrastructure.Sql.Specifications;

namespace Records.Users.Infrastructure.Sql;

public sealed class UserRoleMembershipQueries(UsersDbContext dbContext) : IUserRoleMembershipQueries
{
    public async Task<IReadOnlyList<UserRoleMembershipDto>> ListForUserAsync(
        UlidId userId,
        CancellationToken cancellationToken
    )
    {
        var userKey = userId.ToString();
        var query = dbContext.UserRoleMemberships
            .AsNoTracking()
            .ApplySpecification(new UserRoleMembershipsByUserSpec(userKey));

        return await query
            .Select(x => new UserRoleMembershipDto(
                UlidId.Parse(x.UserId),
                x.Role,
                x.TenantId == null ? null : UlidId.Parse(x.TenantId),
                UlidId.Parse(x.GrantedBy),
                x.GrantedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
