using MediatR;
using Records.Users.Contracts;
using Records.Users.Domain;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Application.Commands.GrantUserRole;

public sealed class GrantUserRoleCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserAccessQueries accessQueries
)
    : IRequestHandler<GrantUserRoleCommand>
{
    public async Task Handle(GrantUserRoleCommand request, CancellationToken cancellationToken)
    {
        var actorId = request.GrantedBy;
        var isGlobalAdmin = await accessQueries.IsGlobalAdminAsync(actorId, cancellationToken);
        var isTenantAdmin = request.TenantId.HasValue &&
                            await accessQueries.IsTenantAdminAsync(actorId, request.TenantId.Value, cancellationToken);

        if (!isGlobalAdmin && !isTenantAdmin)
        {
            throw new InvalidOperationException("Granting roles requires global or tenant admin privileges.");
        }

        if (request.Role == UserRole.GlobalAdmin && !isGlobalAdmin)
        {
            throw new InvalidOperationException("Only global admins can grant global admin access.");
        }

        await unitOfWork.AddRoleMembershipAsync(
            request.UserId,
            request.Role,
            request.TenantId,
            request.GrantedBy,
            request.GrantedAt,
            cancellationToken
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}