using MediatR;
using Records.Core.Application;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands;

public sealed class CreateNotificationCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<CreateNotificationCommand, Result<CreateNotificationResult>>
{
    public async Task<Result<CreateNotificationResult>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken
    )
    {
        var createdAt = DateTimeOffset.UtcNow;

        var notification = Notification.Create(
            request.Trigger,
            request.Recipient,
            request.Content,
            request.Schedule,
            request.Priority,
            createdAt,
            request.CorrelationId);

        await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateNotificationResult(
            notification.Id,
            createdAt,
            notification.ScheduledFor));
    }
}