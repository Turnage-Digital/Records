using Records.Core.Domain.ValueObjects;

namespace Records.Core.Contracts.Security;

public interface ICurrentUserAccess
{
    bool TryGetCurrentUserId(out UlidId userId);
    UlidId GetCurrentUserIdOrThrow();
    Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken);
    Task<bool> CanAccessOpsAsync(CancellationToken cancellationToken);
    Task<bool> CanManageTenantAsync(UlidId tenantId, CancellationToken cancellationToken);
    Task<bool> CanOperateTenantAsync(UlidId tenantId, CancellationToken cancellationToken);
}