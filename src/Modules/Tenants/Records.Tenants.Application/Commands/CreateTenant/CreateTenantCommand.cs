using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Name) : IRequest<UlidId>;