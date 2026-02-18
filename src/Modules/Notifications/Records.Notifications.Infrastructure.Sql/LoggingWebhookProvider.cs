using Microsoft.Extensions.Logging;
using Records.Notifications.Domain;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class LoggingWebhookProvider(ILogger<LoggingWebhookProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Webhook;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Webhook;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug(
            "[STUB WEBHOOK] To: {Url}, Subject: {Subject}",
            recipient.Address,
            content.Subject);

        var fakeMessageId = $"stub-webhook-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}