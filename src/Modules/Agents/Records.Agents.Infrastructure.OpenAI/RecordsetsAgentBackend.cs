using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.Sql;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class RecordsetsAgentBackend(IRecordsetsMcpClient mcpClient) : IAgentBackend
{
    private static readonly HashSet<string> ModelVisibleTools = new(StringComparer.Ordinal)
    {
        "list_recordsets",
        "resolve_record_schema",
        "search_records",
        "get_record",
        "get_record_history",
        "validate_record_create",
        "validate_record_update"
    };

    public string BackendId => "records";

    public async Task<IReadOnlyList<AgentFunctionToolDefinition>> ListModelToolsAsync(CancellationToken cancellationToken)
    {
        return (await mcpClient.ListToolsAsync(cancellationToken))
            .Where(tool => ModelVisibleTools.Contains(tool.Name))
            .Select(tool => new AgentFunctionToolDefinition(tool.Name, tool.Description, tool.JsonSchema))
            .ToArray();
    }

    public async Task<AgentBackendToolExecutionResult> ExecuteModelToolAsync(
        string toolName,
        JsonObject arguments,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken
    )
    {
        if (!ModelVisibleTools.Contains(toolName))
        {
            const string toolUnavailableError = "The requested tool is not available for direct model execution.";
            return new AgentBackendToolExecutionResult(
                CreateErrorJson(toolUnavailableError),
                RecordsetsWorkspaceMapper.CreateToolReceiptJson(toolName, "rejected", null, toolUnavailableError, null, null),
                "rejected",
                Summary: null,
                Error: toolUnavailableError,
                Artifact: null,
                Proposal: null);
        }

        var toolResult = await mcpClient.CallToolAsync(toolName, arguments, callContext, cancellationToken);
        var summary = toolResult.IsError
            ? null
            : RecordsetsWorkspaceMapper.SummarizeToolResult(toolName, toolResult.OutputJson);
        var artifact = toolResult.IsError ? null : RecordsetsWorkspaceMapper.TryMapArtifact(toolName, toolResult.OutputJson);
        var proposal = toolResult.IsError ? null : RecordsetsWorkspaceMapper.TryMapProposal(toolName, toolResult.OutputJson);
        var status = toolResult.IsError ? "failed" : "completed";
        var error = toolResult.IsError ? toolResult.Error ?? toolResult.OutputJson : null;

        return new AgentBackendToolExecutionResult(
            toolResult.OutputJson,
            RecordsetsWorkspaceMapper.CreateToolReceiptJson(toolName, status, summary, error, artifact, proposal),
            status,
            summary,
            error,
            artifact,
            proposal);
    }

    public string? CreatePromptArtifactJson(WorkspaceArtifactDto artifact)
    {
        return RecordsetsWorkspaceMapper.CreateArtifactPromptJson(artifact);
    }

    public string? CreatePromptProposalJson(WorkspaceProposalDto proposal)
    {
        return RecordsetsWorkspaceMapper.CreateProposalPromptJson(proposal);
    }

    public async Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        WorkspaceProposalDto proposal,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken
    )
    {
        var changes = new JsonObject();
        foreach (var diff in proposal.Diffs)
        {
            changes[diff.Key] = diff.After is null
                ? null
                : JsonSerializer.SerializeToNode(diff.After, AgentJsonSerializer.Options);
        }

        var arguments = new JsonObject
        {
            ["collectionId"] = proposal.Target.CollectionId,
            ["changes"] = changes
        };

        var operation = string.Equals(proposal.Kind, "create", StringComparison.OrdinalIgnoreCase)
            ? "apply_record_create"
            : "apply_record_update";

        if (!string.Equals(proposal.Kind, "create", StringComparison.OrdinalIgnoreCase))
        {
            arguments["entityId"] = int.TryParse(proposal.Target.EntityId, out var entityId)
                ? entityId
                : proposal.Target.EntityId;
        }

        var result = await mcpClient.CallToolAsync(operation, arguments, callContext, cancellationToken);
        if (result.IsError)
        {
            throw new InvalidOperationException(result.Error ?? "Failed to apply the confirmed proposal.");
        }

        return RecordsetsWorkspaceMapper.TryMapAppliedEntity(result.OutputJson);
    }

    private static string CreateErrorJson(string error)
    {
        return new JsonObject
        {
            ["error"] = error,
            ["source"] = Activity.Current?.OperationName ?? "records.agents"
        }.ToJsonString();
    }
}
