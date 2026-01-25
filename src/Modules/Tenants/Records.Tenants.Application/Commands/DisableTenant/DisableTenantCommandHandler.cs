using MediatR;
using Records.Tenants.Contracts;
using Records.Tenants.Domain.Interfaces;

namespace Records.Tenants.Application.Commands.DisableTenant;

public sealed class DisableTenantCommandHandler(
    ITenantsUnitOfWork unitOfWork,
    ITenantProjectionWriter projectionWriter
) : IRequestHandler<DisableTenantCommand>
{
    public async Task Handle(DisableTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.GetTenantByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            throw new InvalidOperationException($"Tenant '{request.TenantId}' not found.");
        }

        tenant.Disable();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateStatusAsync(tenant.Id, tenant.Status, cancellationToken);
    }
}