using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.Resume;

public sealed class ResumeClockCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockProjectionWriter projectionWriter
) : IRequestHandler<ResumeClockCommand>
{
    public async Task Handle(ResumeClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.Clocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            throw new InvalidOperationException("Clock not found.");
        }

        clock.Resume(request.ResumedAt);
        await unitOfWork.Clocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateResumeInfoAsync(
            clock.Id,
            clock.State,
            clock.AtRiskDueAt,
            clock.BreachDueAt,
            clock.AccumulatedPauseTime,
            cancellationToken
        );
    }
}