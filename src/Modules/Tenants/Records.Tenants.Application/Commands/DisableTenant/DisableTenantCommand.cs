using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Application.Commands.DisableTenant;

public sealed record DisableTenantCommand(UlidId TenantId) : IRequest;