using MediatR;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.Resume;

public sealed class ResumeClockCommandHandler(
    IClocksUnitOfWork unitOfWork
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
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
