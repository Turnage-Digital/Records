using Records.Tenants.Domain;

namespace Records.Tenants.Contracts;

/// <summary>
///     Writes tenant projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface ITenantProjectionWriter
{
    Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken);

    Task UpdateStatusAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken);

    Task UpdateNameAsync(Guid tenantId, string name, CancellationToken cancellationToken);
}

/// <summary>
///     Model representing the tenant projection data.
/// </summary>
public sealed record TenantProjectionModel(
    Guid TenantId,
    string Name,
    TenantStatus Status,
    DateTimeOffset CreatedAt
);