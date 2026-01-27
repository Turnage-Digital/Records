using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Recordsets.Contracts.Queries;

public interface IRecordsetQueries
{
    Task<RecordsetSummaryDto?> GetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecordsetSummaryDto>> ListAsync(CancellationToken cancellationToken);
}