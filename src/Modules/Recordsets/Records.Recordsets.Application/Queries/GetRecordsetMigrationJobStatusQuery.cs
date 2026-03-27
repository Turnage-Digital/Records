using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Application.Queries;

public record GetRecordsetMigrationJobStatusQuery(UlidId RecordsetId, UlidId CorrelationId)
    : IRequest<RecordsetMigrationProgress?>;

public class GetRecordsetMigrationJobStatusQueryHandler(IRecordsetMigrationJobQueries getter)
    : IRequestHandler<GetRecordsetMigrationJobStatusQuery, RecordsetMigrationProgress?>
{
    public async Task<RecordsetMigrationProgress?> Handle(
        GetRecordsetMigrationJobStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var dto = await getter.GetAsync(request.RecordsetId, request.CorrelationId, cancellationToken);
        if (dto is null)
        {
            return null;
        }

        return new RecordsetMigrationProgress(
            dto.JobId,
            dto.SourceRecordsetId,
            dto.CorrelationId,
            dto.Stage,
            dto.RequestedBy,
            dto.CreatedAt,
            dto.StartedAt,
            dto.CompletedAt,
            dto.BackupRecordsetId,
            dto.NewRecordsetId,
            dto.BackupExpiresAt,
            dto.BackupRemovedAt,
            dto.Attempts,
            dto.LastError
        );
    }
}

public record RecordsetMigrationProgress(
    UlidId JobId,
    UlidId SourceRecordsetId,
    UlidId CorrelationId,
    RecordsetMigrationJobStage Stage,
    UlidId RequestedBy,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    UlidId? BackupRecordsetId,
    UlidId? NewRecordsetId,
    DateTime? BackupExpiresAt,
    DateTime? BackupRemovedAt,
    int Attempts,
    string? LastError
);