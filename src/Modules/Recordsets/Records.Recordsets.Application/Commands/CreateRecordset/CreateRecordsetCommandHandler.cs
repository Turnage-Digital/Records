using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Interfaces;

namespace Records.Recordsets.Application.Commands.CreateRecordset;

public sealed class CreateRecordsetCommandHandler(
    IRecordsetsUnitOfWork unitOfWork,
    IRecordsetProjectionWriter projectionWriter
) : IRequestHandler<CreateRecordsetCommand, UlidId>
{
    public async Task<UlidId> Handle(CreateRecordsetCommand request, CancellationToken cancellationToken)
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            request.Name.Trim(),
            request.CreatedBy,
            request.CreatedAt,
            request.Columns,
            request.Statuses,
            request.StatusTransitions
        );

        await unitOfWork.AddRecordsetAsync(recordset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await projectionWriter.UpsertAsync(
            new RecordsetProjectionModel(recordset.Id, recordset.Name, 0, request.CreatedAt),
            cancellationToken
        );

        return recordset.Id;
    }
}