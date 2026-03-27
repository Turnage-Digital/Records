using MediatR;
using Records.Core.Application;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands;

public sealed record MarkNotificationReadCommand(
    UlidId NotificationId,
    DateTimeOffset ReadAt
) : RequestBase<Result>;

public sealed class MarkNotificationReadCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result.Fail(ResultErrors.Forbidden);
        }

        var notification = await unitOfWork.Notifications.GetByIdAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (!string.Equals(notification.Recipient.UserId, request.UserId, StringComparison.Ordinal))
        {
            return Result.Fail(ResultErrors.Forbidden);
        }

        notification.MarkRead(request.ReadAt);
        await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}