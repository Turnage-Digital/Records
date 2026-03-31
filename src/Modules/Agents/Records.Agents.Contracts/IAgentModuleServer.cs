using System.Text.Json.Nodes;

namespace Records.Agents.Contracts;

public interface IAgentModuleServer
{
    string ModuleId { get; }
    IReadOnlyList<AgentModuleToolDescriptor> Tools { get; }

    Task<JsonNode?> InvokeAsync(
        string operation,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    );
}

public sealed record AgentModuleToolDescriptor(string Name, string Description);
