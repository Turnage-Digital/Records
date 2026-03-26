using MediatR;
using Records.Clocks.Domain;
using Records.Clocks.Domain.ValueObjects;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record UpdateClockDefinitionCommand(
    UlidId DefinitionId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit
) : IRequest;

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
            ClockThreshold.From(request.BreachThresholdValue, request.BreachThresholdUnit)
        );

        await unitOfWork.ClockDefinitions.UpdateAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
