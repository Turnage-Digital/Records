using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Recordsets.Contracts.Queries;

public interface IRecordQueries
{
    Task<RecordDto?> GetByIdAsync(UlidId recordsetId, int itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecordDto>> ListAsync(UlidId recordsetId, CancellationToken cancellationToken);
}