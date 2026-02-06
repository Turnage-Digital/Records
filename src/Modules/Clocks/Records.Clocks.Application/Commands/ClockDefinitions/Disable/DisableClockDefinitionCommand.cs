using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Disable;

public sealed record DisableClockDefinitionCommand(
    UlidId DefinitionId,
    UlidId UpdatedBy,
    DateTimeOffset UpdatedAt
) : IRequest;