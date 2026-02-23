using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts.Projections;
using Records.Tenants.Domain;

namespace Records.Tenants.Application.Commands;

public sealed record CreateTenantCommand(string Name) : IRequest<UlidId>;

public sealed class CreateTenantCommandHandler(
    ITenantsUnitOfWork unitOfWork,
    ITenantProjectionWriter projectionWriter
) : IRequestHandler<CreateTenantCommand, UlidId>
{
    public async Task<UlidId> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Tenant(UlidId.NewUlid(), request.Name);
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