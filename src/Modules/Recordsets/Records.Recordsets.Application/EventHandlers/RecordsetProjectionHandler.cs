using MediatR;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.Events;

namespace Records.Recordsets.Application.EventHandlers;

public sealed class RecordsetProjectionHandler(
    IRecordsetProjectionWriter projectionWriter,
    IRecordsetsUnitOfWork unitOfWork
) : INotificationHandler<RecordsetCreated>,
    INotificationHandler<RecordsetUpdated>
{
    public Task Handle(RecordsetCreated notification, CancellationToken cancellationToken)
    {
        var model = new RecordsetProjectionModel(
            notification.RecordsetId,
            notification.Name,
            0,
            notification.CreatedAt
        );

        return projectionWriter.UpsertAsync(model, cancellationToken);
    }

    public async Task Handle(RecordsetUpdated notification, CancellationToken cancellationToken)
    {
        var recordset = await unitOfWork.GetRecordsetByIdAsync(notification.RecordsetId, cancellationToken);
        if (recordset is null)
        {
            return;
        }

        var itemCount = await unitOfWork.GetRecordCountAsync(notification.RecordsetId, cancellationToken);
        var model = new RecordsetProjectionModel(
            recordset.Id,
            recordset.Name,
            itemCount,
            notification.UpdatedAt
        );

        await projectionWriter.UpsertAsync(model, cancellationToken);
    }
}