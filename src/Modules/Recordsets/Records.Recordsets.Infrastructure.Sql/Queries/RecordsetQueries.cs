using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Infrastructure.Sql.Queries;

public sealed class RecordsetQueries(RecordsetsDbContext dbContext) : IRecordsetQueries
{
    public async Task<RecordsetSummaryDto?> GetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
    {
        var recordsetKey = recordsetId.ToString();
        return await dbContext.RecordsetProjections
            .AsNoTracking()
            .Where(x => x.RecordsetId == recordsetKey)
            .Select(x => new RecordsetSummaryDto(
                UlidId.Parse(x.RecordsetId),
                x.Name,
                x.ItemCount,
                x.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordsetSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RecordsetProjections
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RecordsetSummaryDto(
                UlidId.Parse(x.RecordsetId),
                x.Name,
                x.ItemCount,
                x.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}