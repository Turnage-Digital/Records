using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Contracts.Dtos;

public sealed record UserRoleMembershipDto(
    UlidId UserId,
    UserRole Role,
    UlidId? TenantId,
    UlidId GrantedBy,
    DateTimeOffset GrantedAt
);