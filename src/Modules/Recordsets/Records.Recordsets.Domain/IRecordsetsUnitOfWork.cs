using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Domain;

public interface IRecordsetsUnitOfWork : IUnitOfWork
{
    Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken);
    Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken);
    Task AddRecordsetAsync(Recordset recordset, CancellationToken cancellationToken);
    Task UpdateRecordsetAsync(Recordset recordset, CancellationToken cancellationToken);
    Task DeleteRecordsetAsync(UlidId recordsetId, CancellationToken cancellationToken);
    Task AddRecordAsync(Record record, CancellationToken cancellationToken);
    Task UpdateRecordAsync(Record record, CancellationToken cancellationToken);
    Task<Record?> GetRecordByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken);
    Task<int> GetRecordCountAsync(UlidId recordsetId, CancellationToken cancellationToken);
}