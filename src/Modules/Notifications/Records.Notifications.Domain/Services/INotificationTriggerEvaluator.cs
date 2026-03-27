using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Domain.Services;

public interface INotificationTriggerEvaluator
{
    Task<bool> ShouldTriggerAsync(
        NotificationRule rule,
        NotificationTrigger actualTrigger,
        Dictionary<string, object> context,
        CancellationToken cancellationToken
    );
}