using Microsoft.Extensions.Logging;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class LoggingSmsProvider(ILogger<LoggingSmsProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Sms;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug(
            "[STUB SMS] To: {To}, Body: {Body}",
            recipient.Address,
            content.Body.Length > 100 ? content.Body[..100] + "..." : content.Body);

        var fakeMessageId = $"stub-sms-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}