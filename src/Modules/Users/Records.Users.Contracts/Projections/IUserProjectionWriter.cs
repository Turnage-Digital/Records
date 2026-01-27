using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Contracts.Projections;

/// <summary>
///     Writes user projection data for read model queries.
/// </summary>
public interface IUserProjectionWriter
{
    Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken);
    Task UpdateStatusAsync(UlidId userId, UserStatus status, CancellationToken cancellationToken);
    Task UpdateProfileAsync(UlidId userId, string email, string? displayName, CancellationToken cancellationToken);
}

public sealed record UserProjectionModel(
    UlidId UserId,
    string Email,
    string? DisplayName,
    DateTimeOffset LastUpdatedAt,
    UserStatus Status
);