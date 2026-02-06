using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.MarkBreached;

public sealed class MarkClockBreachedCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockProjectionWriter projectionWriter
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateBreachedAsync(
            clock.Id,
            clock.State,
            request.BreachedAt,
            cancellationToken
        );
    }
}