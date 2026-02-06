using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Disable;

public sealed class DisableClockDefinitionCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockDefinitionProjectionWriter projectionWriter
) : IRequestHandler<DisableClockDefinitionCommand>
{
    public async Task Handle(DisableClockDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            throw new InvalidOperationException("Clock definition not found.");
        }

        definition.Disable(request.UpdatedBy, request.UpdatedAt);
        await unitOfWork.ClockDefinitions.UpdateAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.DisableAsync(definition.Id, request.UpdatedAt, cancellationToken);
    }
}