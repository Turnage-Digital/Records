using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Clocks.Infrastructure.Sql.QueryCriteria;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.QueryCriteria;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClockDefinitionRepository(ClocksDbContext context) : IClockDefinitionRepository
{
    public async Task<ClockDefinition?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken)
    {
        var entity = await context.ClockDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == definitionId.ToString(), cancellationToken);

        return entity is null ? null : ClockDefinitionMapper.ToDomain(entity);
    }

    public async Task<ClockDefinition?> GetByNameAsync(
        UlidId tenantId,
        string name,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClockDefinitionByTenantAndNameCriteria(tenantId.ToString(), name);
        var entity = await context.ClockDefinitions
            .AsNoTracking()
            .ApplyCriteria(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ClockDefinitionMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<ClockDefinition>> ListByTenantAsync(
        UlidId tenantId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClockDefinitionsByTenantCriteria(tenantId.ToString());
        var entities = await context.ClockDefinitions
            .AsNoTracking()
            .ApplyCriteria(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(ClockDefinitionMapper.ToDomain).ToList();
    }

    public Task AddAsync(ClockDefinition definition, CancellationToken cancellationToken)
    {
        context.ClockDefinitions.Add(ClockDefinitionMapper.ToDb(definition));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ClockDefinition definition, CancellationToken cancellationToken)
    {
        context.ClockDefinitions.Update(ClockDefinitionMapper.ToDb(definition));
        return Task.CompletedTask;
    }
}