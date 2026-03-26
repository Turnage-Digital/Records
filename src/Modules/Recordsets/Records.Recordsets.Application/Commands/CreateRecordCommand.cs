using MediatR;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.IntegrationEvents;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Application.Commands;

public sealed record CreateRecordCommand(
    UlidId RecordsetId,
    object Bag
) : IRequest<CreateRecordResult>;

public sealed record CreateRecordResult(
    UlidId RecordsetId,
    int RecordId
);

public sealed class CreateRecordCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordsetProjectionWriter projectionWriter,
    IRecordBagValidator bagValidator,
    IPublisher publisher,
    ITenantContext? tenantContext = null
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

        if (!UlidId.TryParse(tenantContext?.ActorId, out var actorId))
        {
            throw new InvalidOperationException("Current actor ULID was not available.");
        }

        bagValidator.Validate(recordset, request.Bag);
        var occurredAt = DateTimeOffset.UtcNow;

        var record = new Record(0, recordset.Id, request.Bag);
        await unitOfWork.AddRecordAsync(record, actorId, occurredAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new RecordCreatedIntegrationEvent(recordset.Id, record.Id == 0 ? null : record.Id, actorId, occurredAt),
            cancellationToken);

        var itemCount = await unitOfWork.GetRecordCountAsync(recordset.Id, cancellationToken);
        await projectionWriter.UpdateItemCountAsync(recordset.Id, itemCount, cancellationToken);
        await projectionWriter.UpdateLastChangedAsync(recordset.Id, occurredAt, cancellationToken);

        return new CreateRecordResult(recordset.Id, record.Id);
    }
}
