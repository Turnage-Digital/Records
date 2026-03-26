using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record ResumeClockCommand(
    UlidId ClockId,
    DateTimeOffset ResumedAt
) : IRequest;

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
