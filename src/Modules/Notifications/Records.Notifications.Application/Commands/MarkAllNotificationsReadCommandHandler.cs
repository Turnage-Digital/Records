using MediatR;
using Records.Core.Application;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Events;

namespace Records.Notifications.Application.Commands;

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationsUnitOfWork unitOfWork,
    IMediator mediator
) : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result.Fail(ResultErrors.Forbidden);
        }

        var readAt = DateTimeOffset.UtcNow;

        await unitOfWork.Notifications.MarkAllAsReadAsync(
            request.UserId,
            readAt,
            request.Before,
            request.RecordsetId,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await mediator.Publish(
            new AllNotificationsRead(
                request.UserId,
                readAt,
                request.Before,
                request.RecordsetId),
            cancellationToken);

        return Result.Success();
    }
}
