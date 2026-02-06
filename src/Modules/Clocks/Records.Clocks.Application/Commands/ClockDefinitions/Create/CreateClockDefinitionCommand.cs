using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Create;

public sealed record CreateClockDefinitionCommand(
    UlidId TenantId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit,
    UlidId CreatedBy,
    DateTimeOffset CreatedAt
) : IRequest<UlidId>;