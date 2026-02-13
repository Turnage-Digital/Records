using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Recordsets.Contracts.Queries;

public interface IRecordQueries
{
    Task<RecordDto?> GetByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecordDto>> ListAsync(UlidId recordsetId, CancellationToken cancellationToken);
    Task<RecordsetPagedRecordsDto?> GetPageAsync(
        UlidId recordsetId,
        int page,
        int pageSize,
        string? field,
        string? sort,
        CancellationToken cancellationToken
    );
    Task<RecordItemDetailsDto?> GetDetailsAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken);
    Task<HistoryPageDto> GetRecordsetHistoryAsync(
        UlidId recordsetId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
    Task<HistoryPageDto?> GetRecordHistoryAsync(
        UlidId recordsetId,
        int recordId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
