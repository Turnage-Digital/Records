using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;

namespace Records.Users.Contracts.Queries;

public interface IUserQueries
{
    Task<UserSummaryDto?> GetByIdAsync(UlidId userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken);
}