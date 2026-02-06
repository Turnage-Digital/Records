using Microsoft.EntityFrameworkCore;
using Records.Clocks.Domain.Entities;
using Records.Clocks.Domain.Interfaces;
using Records.Clocks.Infrastructure.Sql.Mappers;
using Records.Core.Domain.ValueObjects;

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
        var entity = await context.ClockDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId.ToString() && d.Name == name, cancellationToken);

        return entity is null ? null : ClockDefinitionMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<ClockDefinition>> ListByTenantAsync(
        UlidId tenantId,
        CancellationToken cancellationToken
    )
    {
        var entities = await context.ClockDefinitions
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId.ToString())
            .OrderBy(d => d.Name)
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