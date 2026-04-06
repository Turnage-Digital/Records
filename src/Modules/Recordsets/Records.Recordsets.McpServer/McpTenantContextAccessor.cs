using System.Text.Json.Nodes;
using Records.Core.Contracts;

namespace Records.Recordsets.McpServer;

internal sealed class McpTenantContextAccessor : ITenantContext
{
    private static readonly AsyncLocal<TenantActorContext?> CurrentContext = new();

    public string? TenantId => CurrentContext.Value?.TenantId;
    public string? ActorId => CurrentContext.Value?.ActorId;

    public IDisposable Push(JsonObject? meta)
    {
        var priorContext = CurrentContext.Value;
        CurrentContext.Value = new TenantActorContext(
            ReadMeta(meta, "tenantId"),
            ReadMeta(meta, "actorId"));

        return new RestoreScope(priorContext);
    }

    private static string? ReadMeta(JsonObject? meta, string propertyName)
    {
        return meta?[propertyName]?.GetValue<string>();
    }

    private sealed record TenantActorContext(string? TenantId, string? ActorId);

    private sealed class RestoreScope(TenantActorContext? priorContext) : IDisposable
    {
        public void Dispose()
        {
            CurrentContext.Value = priorContext;
        }
    }
}