using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Domain.ValueObjects;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.ClockDefinitions.Create;

public sealed class CreateClockDefinitionCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IClockDefinitionProjectionWriter projectionWriter
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
            ClockThreshold.From(request.BreachThresholdValue, request.BreachThresholdUnit),
            request.CreatedBy,
            request.CreatedAt
        );

        await unitOfWork.ClockDefinitions.AddAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpsertAsync(
            new ClockDefinitionProjectionModel(
                definition.Id,
                definition.TenantId,
                definition.Name,
                definition.AtRiskThreshold.Value,
                definition.AtRiskThreshold.Unit,
                definition.BreachThreshold.Value,
                definition.BreachThreshold.Unit,
                definition.IsActive,
                request.CreatedAt
            ),
            cancellationToken
        );

        return definition.Id;
    }
}