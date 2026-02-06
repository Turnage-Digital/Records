using MediatR;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands.NotificationRules.Delete;

public sealed class DeleteNotificationRuleCommandHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<DeleteNotificationRuleCommand>
{
    public async Task Handle(DeleteNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await unitOfWork.NotificationRules.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null)
        {
            throw new InvalidOperationException("Notification rule not found.");
        }

        rule.Delete(request.DeletedBy, request.DeletedAt);
        await unitOfWork.NotificationRules.UpdateAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}