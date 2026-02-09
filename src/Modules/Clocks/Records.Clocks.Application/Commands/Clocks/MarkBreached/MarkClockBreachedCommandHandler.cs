using MediatR;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.MarkBreached;

public sealed class MarkClockBreachedCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<MarkClockBreachedCommand>
{
    public async Task Handle(MarkClockBreachedCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.Clocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            throw new InvalidOperationException("Clock not found.");
        }

        clock.MarkBreached(request.BreachedAt);
        await unitOfWork.Clocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
