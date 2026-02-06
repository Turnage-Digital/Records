using Microsoft.Extensions.Logging;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql.Services;

public sealed class LoggingInAppProvider(ILogger<LoggingInAppProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.InApp;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug(
            "[STUB IN-APP] User: {UserId}, Subject: {Subject}",
            recipient.UserId ?? recipient.Address,
            content.Subject);

        var fakeMessageId = $"stub-inapp-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}