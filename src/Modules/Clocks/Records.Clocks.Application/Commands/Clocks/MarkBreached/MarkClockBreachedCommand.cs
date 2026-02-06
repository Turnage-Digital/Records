using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.MarkBreached;

public sealed record MarkClockBreachedCommand(
    UlidId ClockId,
    DateTimeOffset BreachedAt
) : IRequest;