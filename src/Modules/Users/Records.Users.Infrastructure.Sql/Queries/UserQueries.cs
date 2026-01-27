using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;

namespace Records.Users.Infrastructure.Sql.Queries;

public sealed class UserQueries(UsersDbContext dbContext) : IUserQueries
{
    public async Task<UserSummaryDto?> GetByIdAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        return await dbContext.UserProjections
            .AsNoTracking()
            .Where(x => x.UserId == userKey)
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