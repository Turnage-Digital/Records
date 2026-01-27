using MediatR;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services;

namespace Records.Recordsets.Application.Commands.UpdateRecord;

public sealed class UpdateRecordCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordBagValidator bagValidator,
    IRecordsetProjectionWriter projectionWriter
)
    : IRequestHandler<UpdateRecordCommand>
{
    public async Task Handle(UpdateRecordCommand request, CancellationToken cancellationToken)
    {
        var recordset = await unitOfWork.GetRecordsetByIdAsync(request.RecordsetId, cancellationToken);
        if (recordset is null)
        {
            throw new InvalidOperationException($"Recordset '{request.RecordsetId}' not found.");
        }

        var record = await unitOfWork.GetRecordByIdAsync(request.RecordsetId, request.RecordId, cancellationToken);
        if (record is null)
        {
            throw new InvalidOperationException($"Record '{request.RecordId}' not found.");
        }

        bagValidator.Validate(recordset, request.Bag);
        bagValidator.ValidateTransition(recordset, record.Bag, request.Bag);

        record.UpdateBag(request.Bag, request.UpdatedBy, request.UpdatedAt);
        await unitOfWork.UpdateRecordAsync(record, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpdateLastUpdatedAsync(record.RecordsetId, request.UpdatedAt, cancellationToken);
    }
}