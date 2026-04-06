namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class AgentBackendRegistry(IEnumerable<IAgentBackend> backends) : IAgentBackendRegistry
{
    private readonly Dictionary<string, IAgentBackend> _backends = backends.ToDictionary(
        backend => backend.BackendId,
        StringComparer.OrdinalIgnoreCase);

    public string GetDefaultBackendId()
    {
        return _backends.Count switch
        {
            0 => throw new InvalidOperationException("No agent backends are registered."),
            1 => _backends.Keys.Single(),
            _ => throw new InvalidOperationException("Multiple agent backends are registered. Specify a backendId when creating the thread.")
        };
    }

    public IAgentBackend GetRequiredBackend(string? backendId)
    {
        var resolvedBackendId = string.IsNullOrWhiteSpace(backendId)
            ? GetDefaultBackendId()
            : backendId.Trim();

        if (_backends.TryGetValue(resolvedBackendId, out var backend))
        {
            return backend;
        }

        throw new InvalidOperationException($"Unsupported agent backend '{resolvedBackendId}'.");
    }
}
