using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain;

namespace Records.Tenants.Contracts.Dtos;

public sealed record TenantSummaryDto(
    UlidId TenantId,
    string Name,
    TenantStatus Status,
    DateTimeOffset CreatedAt
);