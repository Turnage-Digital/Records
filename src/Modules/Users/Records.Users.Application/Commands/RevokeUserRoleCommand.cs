using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Queries;
using Records.Users.Domain;

namespace Records.Users.Application.Commands;

public sealed record RevokeUserRoleCommand(
    UlidId UserId,
    UserRole Role,
    UlidId? TenantId,
    UlidId RevokedBy
) : IRequest;

public sealed class RevokeUserRoleCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserAccessQueries accessQueries
)
    : IRequestHandler<RevokeUserRoleCommand>
{
    public async Task Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var actorId = request.RevokedBy;
        var isGlobalAdmin = await accessQueries.IsGlobalAdminAsync(actorId, cancellationToken);
        var isTenantAdmin = request.TenantId.HasValue &&
                            await accessQueries.IsTenantAdminAsync(actorId, request.TenantId.Value, cancellationToken);

        if (!isGlobalAdmin && !isTenantAdmin)
        {
            throw new InvalidOperationException("Revoking roles requires global or tenant admin privileges.");
        }

        await unitOfWork.RemoveRoleMembershipAsync(
            request.UserId,
            request.Role,
            request.TenantId,
            cancellationToken
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}