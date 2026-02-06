using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.Pause;

public sealed record PauseClockCommand(
    UlidId ClockId,
    string Reason,
    UlidId PausedBy,
    DateTimeOffset PausedAt
) : IRequest;