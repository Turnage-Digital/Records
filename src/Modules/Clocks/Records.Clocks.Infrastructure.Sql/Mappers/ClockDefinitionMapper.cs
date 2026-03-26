using Records.Clocks.Domain;
using Records.Clocks.Domain.ValueObjects;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql.Mappers;

public static class ClockDefinitionMapper
{
    public static ClockDefinitionDb ToDb(ClockDefinition definition)
    {
        return new ClockDefinitionDb
        {
            Id = definition.Id.ToString(),
            TenantId = definition.TenantId.ToString(),
            Name = definition.Name,
            AtRiskThresholdValue = definition.AtRiskThreshold.Value,
            AtRiskThresholdUnit = (int)definition.AtRiskThreshold.Unit,
            BreachThresholdValue = definition.BreachThreshold.Value,
            BreachThresholdUnit = (int)definition.BreachThreshold.Unit,
            IsActive = definition.IsActive
        };
    }

    public static ClockDefinition ToDomain(ClockDefinitionDb entity)
    {
        return ClockDefinition.Rehydrate(
            UlidId.Parse(entity.Id),
            UlidId.Parse(entity.TenantId),
            entity.Name,
            ClockThreshold.From(entity.AtRiskThresholdValue, (ClockThresholdUnit)entity.AtRiskThresholdUnit),
            ClockThreshold.From(entity.BreachThresholdValue, (ClockThresholdUnit)entity.BreachThresholdUnit),
            entity.IsActive
        );
    }
}