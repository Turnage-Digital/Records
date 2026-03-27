using Records.Clocks.Contracts.Dtos;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Queries;

public interface IClockQueries
{
    Task<ClockDto?> GetByIdAsync(UlidId clockId, CancellationToken cancellationToken);

    Task<ClockDto?> GetByRecordAndDefinitionAsync(
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClockDto>> ListByRecordAsync(
        UlidId recordsetId,
        int recordId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClockWatchdogDto>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClockWatchdogDto>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );
}