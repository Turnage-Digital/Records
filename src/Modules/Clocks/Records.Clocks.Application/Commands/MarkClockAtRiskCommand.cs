using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record MarkClockAtRiskCommand(
    UlidId ClockId,
    DateTimeOffset AtRiskAt
) : IRequest;

public sealed class MarkClockAtRiskCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<MarkClockAtRiskCommand>
{
    public async Task Handle(MarkClockAtRiskCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.Clocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            throw new InvalidOperationException("Clock not found.");
        }

        clock.MarkAtRisk(request.AtRiskAt);
        await unitOfWork.Clocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}