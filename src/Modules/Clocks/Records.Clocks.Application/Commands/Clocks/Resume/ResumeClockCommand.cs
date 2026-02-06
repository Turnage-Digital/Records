using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.Resume;

public sealed record ResumeClockCommand(
    UlidId ClockId,
    UlidId ResumedBy,
    DateTimeOffset ResumedAt
) : IRequest;