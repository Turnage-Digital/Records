using MediatR;

namespace Records.Tenants.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Name) : IRequest<Guid>;