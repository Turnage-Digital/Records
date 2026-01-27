using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Projections;

public interface IRecordsetProjectionWriter
{
    Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken);
    Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken);
    Task UpdateLastUpdatedAsync(UlidId recordsetId, DateTimeOffset updatedAt, CancellationToken cancellationToken);
}

public sealed record RecordsetProjectionModel(
    UlidId RecordsetId,
    string Name,
    int ItemCount,
    DateTimeOffset UpdatedAt
);