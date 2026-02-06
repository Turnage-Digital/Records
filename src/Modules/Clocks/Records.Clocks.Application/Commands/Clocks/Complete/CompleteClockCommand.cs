using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.Complete;

public sealed record CompleteClockCommand(
    UlidId ClockId,
    UlidId CompletedBy,
    DateTimeOffset CompletedAt
) : IRequest;