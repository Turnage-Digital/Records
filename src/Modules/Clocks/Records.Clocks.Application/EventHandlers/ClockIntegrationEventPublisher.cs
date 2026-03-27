using MediatR;
using Records.Clocks.Contracts.IntegrationEvents;
using Records.Clocks.Domain;
using Records.Clocks.Domain.Events;

namespace Records.Clocks.Application.EventHandlers;

public sealed class ClockIntegrationEventPublisher(
    IClocksUnitOfWork unitOfWork,
    IPublisher publisher
) : INotificationHandler<ClockAtRisk>,
    INotificationHandler<ClockBreached>
{
    public async Task Handle(ClockAtRisk notification, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(notification.DefinitionId, cancellationToken);
        var clockName = definition?.Name ?? notification.DefinitionId.ToString();

        await publisher.Publish(
            new ClockAtRiskIntegrationEvent(
                notification.ClockId,
                notification.TenantId,
                notification.RecordsetId,
                notification.RecordId,
                notification.DefinitionId,
                clockName,
                notification.AtRiskAt,
                notification.BreachDueAt),
            cancellationToken);
    }

    public async Task Handle(ClockBreached notification, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(notification.DefinitionId, cancellationToken);
        var clockName = definition?.Name ?? notification.DefinitionId.ToString();

        await publisher.Publish(
            new ClockBreachedIntegrationEvent(
                notification.ClockId,
                notification.TenantId,
                notification.RecordsetId,
                notification.RecordId,
                notification.DefinitionId,
                clockName,
                notification.BreachedAt,
                notification.BreachDueAt),
            cancellationToken);
    }
}