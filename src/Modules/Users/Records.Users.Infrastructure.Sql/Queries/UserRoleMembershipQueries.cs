using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;

namespace Records.Users.Infrastructure.Sql.Queries;

public sealed class UserRoleMembershipQueries(UsersDbContext dbContext) : IUserRoleMembershipQueries
{
    public async Task<IReadOnlyList<UserRoleMembershipDto>> ListForUserAsync(
        UlidId userId,
        CancellationToken cancellationToken
    )
    {
        var userKey = userId.ToString();
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == userKey)
            .OrderByDescending(x => x.GrantedAt)
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