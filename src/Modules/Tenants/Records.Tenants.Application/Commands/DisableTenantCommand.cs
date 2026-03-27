using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain;

namespace Records.Tenants.Application.Commands;

public sealed record DisableTenantCommand(UlidId TenantId) : IRequest;

public sealed class DisableTenantCommandHandler(
    ITenantsUnitOfWork unitOfWork
) : IRequestHandler<DisableTenantCommand>
{
    public async Task Handle(DisableTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.GetTenantByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            throw new InvalidOperationException($"Tenant '{request.TenantId}' not found.");
        }

        tenant.Disable(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}