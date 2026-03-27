using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain;

namespace Records.Tenants.Application.Commands;

public sealed record CreateTenantCommand(string Name) : IRequest<UlidId>;

public sealed class CreateTenantCommandHandler(
    ITenantsUnitOfWork unitOfWork
) : IRequestHandler<CreateTenantCommand, UlidId>
{
    public async Task<UlidId> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(UlidId.NewUlid(), request.Name.Trim(), DateTimeOffset.UtcNow);
        unitOfWork.AddTenant(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}