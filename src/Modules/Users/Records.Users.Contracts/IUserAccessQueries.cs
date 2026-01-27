using Records.Core.Domain.ValueObjects;

namespace Records.Users.Contracts;

/// <summary>
///     Query interface for user access checks. Used by security infrastructure
///     to determine user permissions without direct DbContext dependency.
/// </summary>
public interface IUserAccessQueries
{
    /// <summary>
    ///     Checks if the specified user has global admin privileges.
    /// </summary>
    Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if the specified user has tenant admin privileges for a tenant.
    /// </summary>
    Task<bool> IsTenantAdminAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken);
}