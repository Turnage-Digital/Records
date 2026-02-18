using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Clocks.Infrastructure.Sql.Specifications;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql.Specifications;

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
        var spec = new ClockDefinitionByTenantAndNameSpec(tenantId.ToString(), name);
        var query = context.ClockDefinitions
            .AsNoTracking()
            .ApplySpecification(spec);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ClockDefinitionMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<ClockDefinition>> ListByTenantAsync(
        UlidId tenantId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ClockDefinitionsByTenantSpec(tenantId.ToString());
        var query = context.ClockDefinitions
            .AsNoTracking()
            .ApplySpecification(spec);
        var entities = await query.ToListAsync(cancellationToken);

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
