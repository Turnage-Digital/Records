using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;
using Records.Users.Domain.Entities;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Application.Commands.InviteUser;

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