using MediatR;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Commands.UpdateRecordsetSchema;

public sealed class UpdateRecordsetSchemaCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordsetProjectionWriter projectionWriter
) : IRequestHandler<UpdateRecordsetSchemaCommand>
{
    public async Task Handle(UpdateRecordsetSchemaCommand request, CancellationToken cancellationToken)
    {
        var recordset = await unitOfWork.GetRecordsetByIdAsync(request.RecordsetId, cancellationToken);
        if (recordset is null)
        {
            throw new InvalidOperationException($"Recordset '{request.RecordsetId}' not found.");
        }

        MigrationGuard.ThrowIfRequired(
            recordset.Columns,
            request.Columns,
            recordset.Statuses,
            request.Statuses
        );

        recordset.UpdateSchema(
            request.Columns,
            request.Statuses,
            request.StatusTransitions,
            request.UpdatedBy,
            request.UpdatedAt
        );

        await unitOfWork.UpdateRecordsetAsync(recordset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var itemCount = await unitOfWork.GetRecordCountAsync(recordset.Id, cancellationToken);
        await projectionWriter.UpsertAsync(
            new RecordsetProjectionModel(recordset.Id, recordset.Name, itemCount, request.UpdatedAt),
            cancellationToken
        );
    }
}