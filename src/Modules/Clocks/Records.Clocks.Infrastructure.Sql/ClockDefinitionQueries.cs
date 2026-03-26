using Microsoft.EntityFrameworkCore;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockDefinitionQueries(ClocksDbContext dbContext) : IClockDefinitionQueries
{
    public async Task<ClockDefinitionDto?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ClockDefinitionProjections
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
                entity.IsActive
            );
    }

    public async Task<IReadOnlyList<ClockDefinitionDto>> ListByTenantAsync(
        UlidId tenantId,
        CancellationToken cancellationToken
    )
    {
        var entities = await dbContext.ClockDefinitionProjections
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId.ToString())
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => new ClockDefinitionDto(
                UlidId.Parse(entity.Id),
                UlidId.Parse(entity.TenantId),
                entity.Name,
                entity.AtRiskThresholdValue,
                (ClockThresholdUnit)entity.AtRiskThresholdUnit,
                entity.BreachThresholdValue,
                (ClockThresholdUnit)entity.BreachThresholdUnit,
                entity.IsActive
            ))
            .ToList();
    }
}
