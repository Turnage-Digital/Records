using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;

namespace Records.Notifications.Application.Commands;

public sealed record DeleteNotificationRuleCommand(
    UlidId RuleId
) : IRequest;

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

        rule.Delete();
        await unitOfWork.NotificationRules.UpdateAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
