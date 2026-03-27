using MediatR;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record DisableClockDefinitionCommand(
    UlidId DefinitionId
) : IRequest;

public sealed class DisableClockDefinitionCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<DisableClockDefinitionCommand>
{
    public async Task Handle(DisableClockDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            throw new InvalidOperationException("Clock definition not found.");
        }

        definition.Disable();
        await unitOfWork.ClockDefinitions.UpdateAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}