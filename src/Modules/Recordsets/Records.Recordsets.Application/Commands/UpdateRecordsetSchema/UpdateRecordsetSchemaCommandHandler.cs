using MediatR;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Commands.UpdateRecordsetSchema;

public sealed class UpdateRecordsetSchemaCommandHandler(
    IRecordsetsUnitOfWork unitOfWork
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
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
