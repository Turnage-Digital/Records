using MediatR;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services;

namespace Records.Recordsets.Application.Commands.CreateRecord;

public sealed class CreateRecordCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordsetProjectionWriter projectionWriter,
    IRecordBagValidator bagValidator
) : IRequestHandler<CreateRecordCommand>
{
    public async Task Handle(CreateRecordCommand request, CancellationToken cancellationToken)
    {
        var recordset = await unitOfWork.GetRecordsetByIdAsync(request.RecordsetId, cancellationToken);
        if (recordset is null)
        {
            throw new InvalidOperationException($"Recordset '{request.RecordsetId}' not found.");
        }

        bagValidator.Validate(recordset, request.Bag);

        var record = new Record(0, recordset.Id, request.Bag, request.CreatedBy, request.CreatedAt);
        await unitOfWork.AddRecordAsync(record, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var itemCount = await unitOfWork.GetRecordCountAsync(recordset.Id, cancellationToken);
        await projectionWriter.UpdateItemCountAsync(recordset.Id, itemCount, cancellationToken);
    }
}