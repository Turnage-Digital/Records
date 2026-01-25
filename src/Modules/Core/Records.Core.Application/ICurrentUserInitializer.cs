using Records.Core.Domain.ValueObjects;

namespace Records.Core.Application;

public interface ICurrentUserInitializer
{
    Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken);
}