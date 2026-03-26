using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Queries;

namespace Records.App.Infrastructure.Security;

public sealed class CurrentUserAccess(
    IHttpContextAccessor httpContextAccessor,
    IUserAccessQueries userAccessQueries
) : ICurrentUserAccess, ITenantContext
{
    private static readonly string[] ClaimTypeCandidates =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "userId",
        "id",
        "nameid"
    ];

    private static readonly string[] TenantClaimTypeCandidates =
    [
        "tenantId",
        "tenant_id",
        "tid"
    ];

    public string? TenantId => TryGetTenantId(out var tenantId) ? tenantId : null;

    public string? ActorId => TryGetCurrentUserId(out var userId) ? userId.ToString() : null;

    public bool TryGetCurrentUserId(out UlidId userId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            userId = default;
            return false;
        }

        foreach (var claimType in ClaimTypeCandidates)
        {
            var candidate = user.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(candidate) && UlidId.TryParse(candidate, out userId))
            {
                return true;
            }
        }

        var identityName = user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(identityName) && UlidId.TryParse(identityName, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }

    public UlidId GetCurrentUserIdOrThrow()
    {
        if (TryGetCurrentUserId(out var userId))
        {
            return userId;
        }

        throw new InvalidOperationException("Authenticated user ULID was not available from claims.");
    }

    public async Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return false;
        }

        return await userAccessQueries.IsGlobalAdminAsync(userId, cancellationToken);
    }

    public async Task<bool> CanAccessOpsAsync(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return false;
        }

        if (await userAccessQueries.IsGlobalAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        if (await userAccessQueries.IsTenantAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        return await userAccessQueries.IsOperationsAsync(userId, cancellationToken);
    }

    public async Task<bool> CanManageTenantAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return false;
        }

        if (await userAccessQueries.IsGlobalAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        return await userAccessQueries.IsTenantAdminAsync(userId, tenantId, cancellationToken);
    }

    public async Task<bool> CanOperateTenantAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return false;
        }

        if (await userAccessQueries.IsGlobalAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        if (await userAccessQueries.IsTenantAdminAsync(userId, tenantId, cancellationToken))
        {
            return true;
        }

        return await userAccessQueries.IsOperationsAsync(userId, tenantId, cancellationToken);
    }

    private bool TryGetTenantId(out string tenantId)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            tenantId = string.Empty;
            return false;
        }

        foreach (var claimType in TenantClaimTypeCandidates)
        {
            var candidate = user.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                tenantId = candidate;
                return true;
            }
        }

        tenantId = string.Empty;
        return false;
    }
}
