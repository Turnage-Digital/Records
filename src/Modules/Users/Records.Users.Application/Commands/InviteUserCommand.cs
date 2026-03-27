using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;

namespace Records.Users.Application.Commands;

public sealed record InviteUserCommand(
    string Email,
    string? DisplayName,
    IReadOnlyCollection<InviteUserRole> Roles,
    DateTimeOffset InvitedAt
) : IRequest<UlidId>;

public sealed record InviteUserRole(UserRole Role, UlidId? TenantId);

public sealed class InviteUserCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserProjectionWriter projectionWriter
)
    : IRequestHandler<InviteUserCommand, UlidId>
{
    public async Task<UlidId> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existing = await unitOfWork.GetUserByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"User with email '{normalizedEmail}' already exists.");
        }

        var userId = UlidId.NewUlid();
        var user = new User
        {
            Id = userId.ToString(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            UserName = request.Email.Trim(),
            NormalizedUserName = normalizedEmail,
            DisplayName = request.DisplayName?.Trim(),
            Status = UserStatus.Invited
        };

        await unitOfWork.AddUserAsync(user, cancellationToken);

        foreach (var role in request.Roles)
        {
            await unitOfWork.AddRoleMembershipAsync(
                userId,
                role.Role,
                role.TenantId,
                userId,
                request.InvitedAt,
                cancellationToken
            );
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpsertAsync(
            new UserProjectionModel(
                userId,
                user.Email ?? string.Empty,
                user.DisplayName,
                DateTimeOffset.UtcNow,
                user.Status
            ),
            cancellationToken
        );

        return userId;
    }
}