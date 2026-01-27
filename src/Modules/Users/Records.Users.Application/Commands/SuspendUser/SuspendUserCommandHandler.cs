using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Application.Commands.SuspendUser;

public sealed class SuspendUserCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserProjectionWriter projectionWriter
)
    : IRequestHandler<SuspendUserCommand>
{
    public async Task Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{request.UserId}' not found.");
        }

        user.Status = UserStatus.Suspended;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateStatusAsync(UlidId.Parse(user.Id), user.Status, cancellationToken);
    }
}