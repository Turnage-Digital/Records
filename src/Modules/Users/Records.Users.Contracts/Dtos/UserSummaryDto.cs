using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Contracts.Dtos;

public sealed record UserSummaryDto(
    UlidId UserId,
    string Email,
    string? DisplayName,
    UserStatus Status
);