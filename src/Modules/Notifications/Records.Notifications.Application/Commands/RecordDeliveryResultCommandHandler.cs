using MediatR;
using Records.Core.Application;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands;

public sealed class RecordDeliveryResultCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<RecordDeliveryResultCommand, Result>
{
    public async Task<Result> Handle(
        RecordDeliveryResultCommand request,
        CancellationToken cancellationToken
    )
    {
        var notification = await unitOfWork.Notifications.GetByIdAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var now = DateTimeOffset.UtcNow;

        if (request.Success)
        {
            notification.RecordDeliverySuccess(now, request.ProviderMessageId);
        }
        else if (request.IsPermanentFailure)
        {
            notification.RecordBounce(now, request.ErrorMessage ?? "Permanent failure");
        }
        else
        {
            notification.RecordDeliveryFailure(now, request.ErrorMessage ?? "Delivery failed");
        }

        await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}