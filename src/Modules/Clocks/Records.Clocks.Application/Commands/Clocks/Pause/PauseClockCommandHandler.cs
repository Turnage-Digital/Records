using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.Pause;

public sealed class PauseClockCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockProjectionWriter projectionWriter
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdatePauseInfoAsync(
            clock.Id,
            clock.State,
            request.PausedAt,
            request.Reason,
            cancellationToken
        );
    }
}