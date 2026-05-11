using System.Text.Json.Nodes;
using Records.Agents.Contracts.Dtos;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed record AgentMcpToolDefinition(
    string Name,
    string Description,
    string JsonSchema);

internal sealed record AgentMcpCallContext(
    string? TenantId,
    string? ActorId,
    string? CorrelationId);

internal sealed record AgentMcpToolCallResult(
    string OutputJson,
    bool IsError,
    string? Error);

internal sealed record AgentBackendToolExecutionResult(
    string OutputJson,
    string ModelReceiptJson,
    string Status,
    string? Summary,
    string? Error,
    WorkspaceArtifactDto? Artifact,
    WorkspaceProposalDto? Proposal);

internal sealed record AgentFunctionToolDefinition(
    string Name,
    string Description,
    string JsonSchema);

internal sealed record AgentFunctionCallRequest(
    string CallId,
    string Name,
    string ArgumentsJson);

internal sealed record AgentFunctionCallOutput(
    string CallId,
    string ReceiptJson);

internal sealed record AgentLlmConversationContext(
    string BackendId,
    string ThreadId,
    string Message,
    string? PastedText,
    string? CurrentArtifactJson,
    string? PendingProposalJson);

internal sealed record AgentLlmRequest(
    AgentLlmConversationContext? Conversation,
    string? PreviousResponseId,
    IReadOnlyList<AgentFunctionToolDefinition> Tools,
    IReadOnlyList<AgentFunctionCallOutput> ToolOutputs);

internal sealed record AgentLlmResponse(
    string? ResponseId,
    string AssistantMessage,
    IReadOnlyList<AgentFunctionCallRequest> ToolCalls);

internal interface IAgentLlmClient
{
    Task<AgentLlmResponse> CreateResponseAsync(
        AgentLlmRequest request,
        Func<string, CancellationToken, Task>? onTextDelta,
        CancellationToken cancellationToken);
}

internal interface IRecordsetsMcpClient
{
    Task<IReadOnlyList<AgentMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken);

    Task<AgentMcpToolCallResult> CallToolAsync(
        string toolName,
        JsonObject arguments,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken);
}

internal interface IAgentBackend
{
    string BackendId { get; }

    Task<IReadOnlyList<AgentFunctionToolDefinition>> ListModelToolsAsync(CancellationToken cancellationToken);

    Task<AgentBackendToolExecutionResult> ExecuteModelToolAsync(
        string toolName,
        JsonObject arguments,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken);

    string? CreatePromptArtifactJson(WorkspaceArtifactDto artifact);

    string? CreatePromptProposalJson(WorkspaceProposalDto proposal);

    Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        WorkspaceProposalDto proposal,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken);
}

internal interface IAgentBackendRegistry
{
    string GetDefaultBackendId();
    IAgentBackend GetRequiredBackend(string? backendId);
}

internal interface IAgentProposalExecutor
{
    Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
        string backendId,
        WorkspaceProposalDto proposal,
        CancellationToken cancellationToken);
}
