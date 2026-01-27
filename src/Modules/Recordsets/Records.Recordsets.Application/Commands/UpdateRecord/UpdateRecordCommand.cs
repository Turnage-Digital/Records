using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands.UpdateRecord;

public sealed record UpdateRecordCommand(
    UlidId RecordsetId,
    int RecordId,
    object Bag,
    UlidId UpdatedBy,
    DateTimeOffset UpdatedAt
) : IRequest;