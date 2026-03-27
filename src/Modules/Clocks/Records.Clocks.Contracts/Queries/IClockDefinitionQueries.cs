using Records.Clocks.Contracts.Dtos;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Queries;

public interface IClockDefinitionQueries
{
    Task<ClockDefinitionDto?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClockDefinitionDto>> ListByTenantAsync(UlidId tenantId, CancellationToken cancellationToken);
}