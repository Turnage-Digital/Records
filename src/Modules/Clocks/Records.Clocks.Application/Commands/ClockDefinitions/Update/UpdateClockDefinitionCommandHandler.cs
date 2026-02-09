using MediatR;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Update;

public sealed class UpdateClockDefinitionCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<UpdateClockDefinitionCommand>
{
    public async Task Handle(UpdateClockDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            throw new InvalidOperationException("Clock definition not found.");
        }

        definition.Update(
            request.Name.Trim(),
            ClockThreshold.From(request.AtRiskThresholdValue, request.AtRiskThresholdUnit),
            ClockThreshold.From(request.BreachThresholdValue, request.BreachThresholdUnit),
            request.UpdatedBy,
            request.UpdatedAt
        );

        await unitOfWork.ClockDefinitions.UpdateAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
