using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Application.Queries.Migrations;

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
            dto.CreatedOn,
            dto.StartedOn,
            dto.CompletedOn,
            dto.BackupRecordsetId,
            dto.NewRecordsetId,
            dto.BackupExpiresOn,
            dto.BackupRemovedOn,
            dto.Attempts,
            dto.LastError
        );
    }
}