using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands.CreateRecordset;

public sealed record CreateRecordsetCommand(
    string Name,
    IReadOnlyList<Column> Columns,
    IReadOnlyList<Status> Statuses,
    IReadOnlyList<StatusTransition> StatusTransitions,
    UlidId CreatedBy,
    DateTimeOffset CreatedAt
) : IRequest<UlidId>;