using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record CompleteClockCommand(
    UlidId ClockId,
    DateTimeOffset CompletedAt
) : IRequest;

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
