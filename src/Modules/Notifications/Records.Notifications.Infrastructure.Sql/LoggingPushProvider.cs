using Microsoft.Extensions.Logging;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Services;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class LoggingPushProvider(ILogger<LoggingPushProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Push;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug(
            "[STUB PUSH] Device: {Device}, Subject: {Subject}",
            recipient.Address,
            content.Subject);

        var fakeMessageId = $"stub-push-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}