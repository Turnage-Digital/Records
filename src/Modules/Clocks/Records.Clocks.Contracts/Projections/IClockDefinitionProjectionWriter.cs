using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Projections;

public interface IClockDefinitionProjectionWriter
{
    Task UpsertAsync(ClockDefinitionProjectionModel model, CancellationToken cancellationToken);
    Task DisableAsync(UlidId definitionId, CancellationToken cancellationToken);
}

public sealed record ClockDefinitionProjectionModel(
    UlidId DefinitionId,
    UlidId TenantId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit,
    bool IsActive
);