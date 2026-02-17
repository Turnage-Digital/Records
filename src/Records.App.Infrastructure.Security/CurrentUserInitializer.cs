using Records.Core.Application;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;

namespace Records.App.Infrastructure.Security;

public sealed class CurrentUserInitializer(ICurrentUserAccess currentUserAccess) : ICurrentUserInitializer
{
    public Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(currentUserAccess.GetCurrentUserIdOrThrow());
    }
}
