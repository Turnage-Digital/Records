using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;

namespace Records.Users.Contracts.Queries;

public interface IUserRoleMembershipQueries
{
    Task<IReadOnlyList<UserRoleMembershipDto>> ListForUserAsync(UlidId userId, CancellationToken cancellationToken);
}