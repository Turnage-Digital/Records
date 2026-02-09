using MediatR;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.Complete;

public sealed class CompleteClockCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<CompleteClockCommand>
{
    public async Task Handle(CompleteClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.Clocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            throw new InvalidOperationException("Clock not found.");
        }

        clock.Complete(request.CompletedAt);
        await unitOfWork.Clocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
