using MediatR;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.IntegrationEvents;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Application.Commands;

public sealed record UpdateRecordCommand(
    UlidId RecordsetId,
    int RecordId,
    object Bag
) : IRequest;

public sealed class UpdateRecordCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordBagValidator bagValidator,
    IRecordsetProjectionWriter projectionWriter,
    IPublisher publisher,
    ITenantContext? tenantContext = null
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

        if (!UlidId.TryParse(tenantContext?.ActorId, out var actorId))
        {
            throw new InvalidOperationException("Current actor ULID was not available.");
        }

        bagValidator.Validate(recordset, request.Bag);
        bagValidator.ValidateTransition(recordset, record.Bag, request.Bag);

        var previousBag = record.Bag;
        var occurredAt = DateTimeOffset.UtcNow;

        record.UpdateBag(request.Bag);
        await unitOfWork.UpdateRecordAsync(record, actorId, occurredAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new RecordUpdatedIntegrationEvent(
                record.RecordsetId,
                record.Id == 0 ? null : record.Id,
                actorId,
                occurredAt,
                previousBag,
                request.Bag),
            cancellationToken);

        await projectionWriter.UpdateLastChangedAsync(record.RecordsetId, occurredAt, cancellationToken);
    }
}