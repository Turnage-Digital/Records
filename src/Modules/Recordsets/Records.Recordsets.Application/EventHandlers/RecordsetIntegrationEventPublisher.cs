using MediatR;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.IntegrationEvents;
using Records.Recordsets.Domain.Events;

namespace Records.Recordsets.Application.EventHandlers;

public sealed class RecordsetIntegrationEventPublisher(
    IPublisher publisher,
    ITenantContext? tenantContext = null
) : INotificationHandler<RecordsetCreated>,
    INotificationHandler<RecordsetUpdated>
{
    public Task Handle(RecordsetCreated notification, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
        {
            return Task.CompletedTask;
        }

        return publisher.Publish(
            new RecordsetCreatedIntegrationEvent(
                notification.RecordsetId,
                actorId,
                notification.OccurredAt),
            cancellationToken);
    }

    public Task Handle(RecordsetUpdated notification, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId))
        {
            return Task.CompletedTask;
        }

        return publisher.Publish(
            new RecordsetUpdatedIntegrationEvent(
                notification.RecordsetId,
                actorId,
                notification.OccurredAt),
            cancellationToken);
    }

    private bool TryGetActorId(out UlidId actorId)
    {
        actorId = default;
        return UlidId.TryParse(tenantContext?.ActorId, out actorId);
    }
}