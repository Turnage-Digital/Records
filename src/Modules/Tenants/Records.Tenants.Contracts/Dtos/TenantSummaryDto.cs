using Records.Tenants.Domain;

namespace Records.Tenants.Contracts.Dtos;

public sealed record TenantSummaryDto(
    Guid TenantId,
    string Name,
    TenantStatus Status,
    DateTimeOffset CreatedAt
);
