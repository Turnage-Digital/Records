using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Update;

public sealed record UpdateClockDefinitionCommand(
    UlidId DefinitionId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit,
    UlidId UpdatedBy,
    DateTimeOffset UpdatedAt
) : IRequest;