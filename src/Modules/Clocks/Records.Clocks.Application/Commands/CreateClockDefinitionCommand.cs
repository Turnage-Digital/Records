using MediatR;
using Records.Clocks.Domain;
using Records.Clocks.Domain.ValueObjects;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record CreateClockDefinitionCommand(
    UlidId TenantId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit
) : IRequest<UlidId>;

public sealed class CreateClockDefinitionCommandHandler(
    IClocksUnitOfWork unitOfWork
) : IRequestHandler<CreateClockDefinitionCommand, UlidId>
{
    public async Task<UlidId> Handle(CreateClockDefinitionCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.ClockDefinitions.GetByNameAsync(
            request.TenantId,
            request.Name.Trim(),
            cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Clock definition name must be unique per tenant.");
        }

        var definition = ClockDefinition.Create(
            UlidId.NewUlid(),
            request.TenantId,
            request.Name.Trim(),
            ClockThreshold.From(request.AtRiskThresholdValue, request.AtRiskThresholdUnit),
            ClockThreshold.From(request.BreachThresholdValue, request.BreachThresholdUnit)
        );

        await unitOfWork.ClockDefinitions.AddAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return definition.Id;
    }
}