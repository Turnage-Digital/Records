using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands.UpdateRecordsetSchema;

public sealed record UpdateRecordsetSchemaCommand(
    UlidId RecordsetId,
    IReadOnlyList<Column> Columns,
    IReadOnlyList<Status> Statuses,
    IReadOnlyList<StatusTransition> StatusTransitions,
    UlidId UpdatedBy,
    DateTimeOffset UpdatedAt
) : IRequest;