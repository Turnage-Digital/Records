using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands;

public sealed record UpdateRecordsetSchemaCommand(
    UlidId RecordsetId,
    IReadOnlyList<Column> Columns,
    IReadOnlyList<Status> Statuses,
    IReadOnlyList<StatusTransition> StatusTransitions
) : IRequest;

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
            DateTimeOffset.UtcNow
        );

        await unitOfWork.UpdateRecordsetAsync(recordset, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}