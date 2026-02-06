using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.MarkAtRisk;

public sealed record MarkClockAtRiskCommand(
    UlidId ClockId,
    DateTimeOffset AtRiskAt
) : IRequest;