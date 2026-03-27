using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Tenants.Domain.Events;

public sealed record TenantDisabled(
    UlidId TenantId
) : INotification;