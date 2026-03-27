using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain;

public interface IClockDefinitionRepository
{
    Task<ClockDefinition?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken);
    Task<ClockDefinition?> GetByNameAsync(UlidId tenantId, string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClockDefinition>> ListByTenantAsync(UlidId tenantId, CancellationToken cancellationToken);
    Task AddAsync(ClockDefinition definition, CancellationToken cancellationToken);
    Task UpdateAsync(ClockDefinition definition, CancellationToken cancellationToken);
}