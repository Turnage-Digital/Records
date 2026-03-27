using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;
using Records.Users.Infrastructure.Sql.QueryCriteria;

namespace Records.Users.Infrastructure.Sql;

public sealed class UserQueries(UsersDbContext dbContext) : IUserQueries
{
    public async Task<UserSummaryDto?> GetByIdAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        return await dbContext.UserProjections
            .AsNoTracking()
            .ApplyCriteria(new UserProjectionByUserIdCriteria(userKey))
            .Select(x => new UserSummaryDto(
                UlidId.Parse(x.UserId),
                x.Email ?? string.Empty,
                x.DisplayName,
                x.Status
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.UserProjections
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(x => new UserSummaryDto(
                UlidId.Parse(x.UserId),
                x.Email ?? string.Empty,
                x.DisplayName,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}