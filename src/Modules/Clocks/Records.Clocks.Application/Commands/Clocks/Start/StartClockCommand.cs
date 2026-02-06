using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands.Clocks.Start;

public sealed record StartClockCommand(
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    UlidId StartedBy,
    DateTimeOffset StartedAt
) : IRequest<UlidId>;