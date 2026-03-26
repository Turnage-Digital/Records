using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record PauseClockCommand(
    UlidId ClockId,
    string Reason,
    DateTimeOffset PausedAt
) : IRequest;

public sealed class PauseClockCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<PauseClockCommand>
{
    public async Task Handle(PauseClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.Clocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            throw new InvalidOperationException("Clock not found.");
        }

        clock.Pause(request.Reason, request.PausedAt);
        await unitOfWork.Clocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
