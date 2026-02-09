using MediatR;
using Records.Recordsets.Contracts.IntegrationEvents;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services;

namespace Records.Recordsets.Application.Commands.CreateRecord;

public sealed class CreateRecordCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordsetProjectionWriter projectionWriter,
    IRecordBagValidator bagValidator,
    IPublisher publisher
) : IRequestHandler<CreateRecordCommand, CreateRecordResult>
{
    public async Task<CreateRecordResult> Handle(
        CreateRecordCommand request,
        CancellationToken cancellationToken
    )
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

        await publisher.Publish(
            new RecordCreatedIntegrationEvent(recordset.Id, record.Id == 0 ? null : record.Id, request.CreatedBy,
                request.CreatedAt),
            cancellationToken);

        var itemCount = await unitOfWork.GetRecordCountAsync(recordset.Id, cancellationToken);
        await projectionWriter.UpdateItemCountAsync(recordset.Id, itemCount, cancellationToken);

        return new CreateRecordResult(recordset.Id, record.Id, request.CreatedAt);
    }
}
