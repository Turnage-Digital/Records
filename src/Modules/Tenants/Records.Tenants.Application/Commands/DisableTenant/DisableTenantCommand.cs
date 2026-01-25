using MediatR;

namespace Records.Tenants.Application.Commands.DisableTenant;

public sealed record DisableTenantCommand(Guid TenantId) : IRequest;