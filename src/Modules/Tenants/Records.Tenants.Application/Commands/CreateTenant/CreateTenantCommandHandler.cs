using MediatR;
using Records.Tenants.Contracts;
using Records.Tenants.Domain.Entities;
using Records.Tenants.Domain.Interfaces;

namespace Records.Tenants.Application.Commands.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantsUnitOfWork unitOfWork,
    ITenantProjectionWriter projectionWriter
) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Tenant(Guid.NewGuid(), request.Name);
        unitOfWork.AddTenant(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpsertAsync(
            new TenantProjectionModel(
                tenant.Id,
                tenant.Name,
                tenant.Status,
                tenant.CreatedAt
            ),
            cancellationToken
        );

        return tenant.Id;
    }
}
