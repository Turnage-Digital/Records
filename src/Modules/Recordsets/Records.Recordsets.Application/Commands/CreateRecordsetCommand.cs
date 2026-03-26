using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands;

public sealed record CreateRecordsetCommand(
    string Name,
    IReadOnlyList<Column> Columns,
    IReadOnlyList<Status> Statuses,
    IReadOnlyList<StatusTransition> StatusTransitions
) : IRequest<UlidId>;

public sealed class CreateRecordsetCommandHandler(
    IRecordsetsUnitOfWork unitOfWork
) : IRequestHandler<CreateRecordsetCommand, UlidId>
{
    public async Task<UlidId> Handle(CreateRecordsetCommand request, CancellationToken cancellationToken)
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            request.Name.Trim(),
            request.Columns,
            request.Statuses,
            request.StatusTransitions,
            DateTimeOffset.UtcNow
        );

        await unitOfWork.AddRecordsetAsync(recordset, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return recordset.Id;
    }
}
