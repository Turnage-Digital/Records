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
            IsActive = definition.IsActive,
            CreatedBy = definition.CreatedBy.ToString(),
            CreatedAt = definition.CreatedAt.UtcDateTime,
            UpdatedBy = definition.UpdatedBy?.ToString(),
            UpdatedAt = definition.UpdatedAt?.UtcDateTime
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
            entity.IsActive,
            UlidId.Parse(entity.CreatedBy),
            new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
            entity.UpdatedBy is null ? null : UlidId.Parse(entity.UpdatedBy),
            entity.UpdatedAt.HasValue ? new DateTimeOffset(entity.UpdatedAt.Value, TimeSpan.Zero) : null
        );
    }
}