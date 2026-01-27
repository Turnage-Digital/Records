using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Application.Commands.RevokeUserRole;

public sealed record RevokeUserRoleCommand(
    UlidId UserId,
    UserRole Role,
    UlidId? TenantId,
    UlidId RevokedBy
) : IRequest;