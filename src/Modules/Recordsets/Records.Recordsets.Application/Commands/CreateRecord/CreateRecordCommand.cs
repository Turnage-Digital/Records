using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Application.Commands.CreateRecord;

public sealed record CreateRecordCommand(
    UlidId RecordsetId,
    object Bag,
    UlidId CreatedBy,
    DateTimeOffset CreatedAt
) : IRequest;