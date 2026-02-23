using Microsoft.Extensions.Logging;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Services;
using Records.Notifications.Domain.ValueObjects;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class LoggingEmailProvider(ILogger<LoggingEmailProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Email;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug(
            "[STUB EMAIL] To: {To} ({DisplayName}), Subject: {Subject}, Body: {Body}, TemplateId: {TemplateId}",
            recipient.Address,
            recipient.DisplayName,
            content.Subject,
            content.Body.Length > 100 ? content.Body[..100] + "..." : content.Body,
            content.TemplateId);

        var fakeMessageId = $"stub-email-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}