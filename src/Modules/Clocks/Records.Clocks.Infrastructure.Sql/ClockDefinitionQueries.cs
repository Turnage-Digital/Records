using Microsoft.EntityFrameworkCore;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.Specifications;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockDefinitionQueries(ClocksDbContext dbContext) : IClockDefinitionQueries
{
    public async Task<ClockDefinitionDto?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ClockDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == definitionId.ToString(), cancellationToken);

        return entity is null
            ? null
            : new ClockDefinitionDto(
                UlidId.Parse(entity.Id),
                UlidId.Parse(entity.TenantId),
                entity.Name,
                entity.AtRiskThresholdValue,
                (ClockThresholdUnit)entity.AtRiskThresholdUnit,
                entity.BreachThresholdValue,
                (ClockThresholdUnit)entity.BreachThresholdUnit,
                entity.IsActive,
                new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
                entity.UpdatedAt.HasValue ? new DateTimeOffset(entity.UpdatedAt.Value, TimeSpan.Zero) : null
            );
    }

    public async Task<IReadOnlyList<ClockDefinitionDto>> ListByTenantAsync(
        UlidId tenantId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClockDefinitionsByTenantSpec(tenantId.ToString());
        var query = dbContext.ClockDefinitions
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

        return entities.Select(entity => new ClockDefinitionDto(
                UlidId.Parse(entity.Id),
                UlidId.Parse(entity.TenantId),
                entity.Name,
                entity.AtRiskThresholdValue,
                (ClockThresholdUnit)entity.AtRiskThresholdUnit,
                entity.BreachThresholdValue,
                (ClockThresholdUnit)entity.BreachThresholdUnit,
                entity.IsActive,
                new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
                entity.UpdatedAt.HasValue ? new DateTimeOffset(entity.UpdatedAt.Value, TimeSpan.Zero) : null
            ))
            .ToList();
    }
}
