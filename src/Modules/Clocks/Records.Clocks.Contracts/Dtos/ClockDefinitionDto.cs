using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Dtos;

public sealed record ClockDefinitionDto(
    UlidId DefinitionId,
    UlidId TenantId,
    string Name,
    int AtRiskThresholdValue,
    ClockThresholdUnit AtRiskThresholdUnit,
    int BreachThresholdValue,
    ClockThresholdUnit BreachThresholdUnit,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);