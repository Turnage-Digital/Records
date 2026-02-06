using Records.Clocks.Domain.Entities;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Interfaces;

public interface IClockRepository
{
    Task<Clock?> GetByIdAsync(UlidId clockId, CancellationToken cancellationToken);

    Task<Clock?> GetByRecordAndDefinitionAsync(
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Clock>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Clock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Clock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );

    Task AddAsync(Clock clock, CancellationToken cancellationToken);
    Task UpdateAsync(Clock clock, CancellationToken cancellationToken);
}