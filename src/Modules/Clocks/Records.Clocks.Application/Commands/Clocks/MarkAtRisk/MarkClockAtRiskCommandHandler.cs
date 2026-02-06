using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.Clocks.MarkAtRisk;

public sealed class MarkClockAtRiskCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockProjectionWriter projectionWriter
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateAtRiskAsync(
            clock.Id,
            clock.State,
            request.AtRiskAt,
            cancellationToken
        );
    }
}