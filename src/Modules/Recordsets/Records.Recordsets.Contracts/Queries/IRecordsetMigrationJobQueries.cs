using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Recordsets.Contracts.Queries;

public interface IRecordsetMigrationJobQueries
{
    Task<RecordsetMigrationJobStatusDto?> GetAsync(
        UlidId recordsetId,
        UlidId correlationId,
        CancellationToken cancellationToken
    );
}