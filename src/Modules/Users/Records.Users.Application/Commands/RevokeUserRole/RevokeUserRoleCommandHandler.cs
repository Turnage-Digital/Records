using MediatR;
using Records.Users.Contracts;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Application.Commands.RevokeUserRole;

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