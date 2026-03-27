using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record MarkClockBreachedCommand(
    UlidId ClockId,
    DateTimeOffset BreachedAt
) : IRequest;

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