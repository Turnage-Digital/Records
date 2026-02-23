using MediatR;

namespace Records.App.Server.Services;

public sealed class ChangeFeedNotificationHandler<TNotification>(
    ChangeFeed feed
) : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public Task Handle(TNotification notification, CancellationToken cancellationToken)
    {
        return feed.PublishAsync(notification, cancellationToken).AsTask();
    }
}