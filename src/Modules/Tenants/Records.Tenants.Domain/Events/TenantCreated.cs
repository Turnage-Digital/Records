using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Domain.Events;

public sealed record TenantCreated(
    UlidId TenantId,
    string Name,
    TenantStatus Status,
    DateTimeOffset OccurredAt
) : INotification;