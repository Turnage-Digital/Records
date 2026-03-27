using MediatR;
using Records.Tenants.Contracts.Projections;
using Records.Tenants.Domain;
using Records.Tenants.Domain.Events;

namespace Records.Tenants.Application.EventHandlers;

public sealed class TenantProjectionHandler(ITenantProjectionWriter projectionWriter)
    : INotificationHandler<TenantCreated>,
        INotificationHandler<TenantDisabled>
{
    public Task Handle(TenantCreated notification, CancellationToken cancellationToken)
    {
        var model = new TenantProjectionModel(
            notification.TenantId,
            notification.Name,
            notification.Status,
            notification.OccurredAt
        );

        return projectionWriter.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(TenantDisabled notification, CancellationToken cancellationToken)
    {
        return projectionWriter.UpdateStatusAsync(
            notification.TenantId,
            TenantStatus.Disabled,
            cancellationToken
        );
    }
}