using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Infrastructure.Sql.Queries;

public sealed class RecordQueries(RecordsetsDbContext dbContext) : IRecordQueries
{
    public async Task<RecordDto?> GetByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey && x.Id == recordId)
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordDto>> ListAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetItems
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecordDto(
                (int)x.Id,
                UlidId.Parse(x.RecordsetId),
                x.BagJson,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}