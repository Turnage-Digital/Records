using MediatR;
using Records.Recordsets.Contracts.IntegrationEvents;
using Records.Recordsets.Domain.Events;

namespace Records.Recordsets.Application.EventHandlers;

public sealed class RecordsetIntegrationEventPublisher(
    IPublisher publisher
) : INotificationHandler<RecordsetCreated>,
    INotificationHandler<RecordsetUpdated>
{
    public Task Handle(RecordsetCreated notification, CancellationToken cancellationToken)
    {
        return publisher.Publish(
            new RecordsetCreatedIntegrationEvent(
                notification.RecordsetId,
                notification.CreatedBy,
                notification.CreatedAt),
            cancellationToken);
    }

    public Task Handle(RecordsetUpdated notification, CancellationToken cancellationToken)
    {
        return publisher.Publish(
            new RecordsetUpdatedIntegrationEvent(
                notification.RecordsetId,
                notification.UpdatedBy,
                notification.UpdatedAt),
            cancellationToken);
    }
}
