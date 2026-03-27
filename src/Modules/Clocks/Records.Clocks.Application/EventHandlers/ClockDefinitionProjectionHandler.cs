using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Events;

namespace Records.Clocks.Application.EventHandlers;

public sealed class ClockDefinitionProjectionHandler(IClockDefinitionProjectionWriter writer)
    : INotificationHandler<ClockDefinitionCreated>,
        INotificationHandler<ClockDefinitionUpdated>,
        INotificationHandler<ClockDefinitionDisabled>
{
    public Task Handle(ClockDefinitionCreated notification, CancellationToken cancellationToken)
    {
        var model = new ClockDefinitionProjectionModel(
            notification.DefinitionId,
            notification.TenantId,
            notification.Name,
            notification.AtRiskThreshold.Value,
            notification.AtRiskThreshold.Unit,
            notification.BreachThreshold.Value,
            notification.BreachThreshold.Unit,
            notification.IsActive
        );

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(ClockDefinitionDisabled notification, CancellationToken cancellationToken)
    {
        return writer.DisableAsync(notification.DefinitionId, cancellationToken);
    }

    public Task Handle(ClockDefinitionUpdated notification, CancellationToken cancellationToken)
    {
        var model = new ClockDefinitionProjectionModel(
            notification.DefinitionId,
            notification.TenantId,
            notification.Name,
            notification.AtRiskThreshold.Value,
            notification.AtRiskThreshold.Unit,
            notification.BreachThreshold.Value,
            notification.BreachThreshold.Unit,
            notification.IsActive
        );

        return writer.UpsertAsync(model, cancellationToken);
    }
}