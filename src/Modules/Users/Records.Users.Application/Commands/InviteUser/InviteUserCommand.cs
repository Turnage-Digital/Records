using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.Users.Application.Commands.InviteUser;

public sealed record InviteUserCommand(
    string Email,
    string? DisplayName,
    IReadOnlyCollection<InviteUserRole> Roles,
    DateTimeOffset InvitedAt
) : IRequest<UlidId>;

public sealed record InviteUserRole(UserRole Role, UlidId? TenantId);