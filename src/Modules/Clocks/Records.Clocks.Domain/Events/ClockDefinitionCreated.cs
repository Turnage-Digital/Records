using MediatR;
using Records.Clocks.Domain.ValueObjects;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain.Events;

public sealed record ClockDefinitionCreated(
    UlidId DefinitionId,
    UlidId TenantId,
    string Name,
    ClockThreshold AtRiskThreshold,
    ClockThreshold BreachThreshold,
    bool IsActive
) : INotification;