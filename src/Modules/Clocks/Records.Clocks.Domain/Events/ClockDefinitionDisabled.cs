using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockDefinitionDisabled(
    UlidId DefinitionId,
    UlidId TenantId
) : INotification;