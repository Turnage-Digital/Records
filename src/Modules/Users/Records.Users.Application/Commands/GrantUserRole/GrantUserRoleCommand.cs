using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Application.Commands.GrantUserRole;

public sealed record GrantUserRoleCommand(
    UlidId UserId,
    UserRole Role,
    UlidId? TenantId,
    UlidId GrantedBy,
    DateTimeOffset GrantedAt
) : IRequest;