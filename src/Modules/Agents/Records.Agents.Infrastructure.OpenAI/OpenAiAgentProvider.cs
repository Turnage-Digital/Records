using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Contracts;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class OpenAiAgentProvider(
    IAgentLlmClient llmClient,
    IAgentBackendRegistry backendRegistry,
    IOptions<AgentLlmOptions> options,
    ITenantContext tenantContext,
    ILogger<OpenAiAgentProvider> logger
) : IAgentProvider
{
    public async Task<AgentTurnResultDto> ExecuteTurnAsync(
        AgentProviderContextDto context,
        Func<AgentStreamEventDto, CancellationToken, Task>? onProgress,
        CancellationToken cancellationToken
    )
    {
        var backend = backendRegistry.GetRequiredBackend(context.BackendId);
        var availableTools = (await backend.ListModelToolsAsync(cancellationToken)).ToArray();

        var currentRequest = new AgentLlmRequest(
            new AgentLlmConversationContext(
                backend.BackendId,
                context.ThreadId,
                context.Message,
                context.PastedText,
                context.CurrentArtifact is null ? null : backend.CreatePromptArtifactJson(context.CurrentArtifact),
                context.PendingProposal is null ? null : backend.CreatePromptProposalJson(context.PendingProposal)),
            PreviousResponseId: null,
            Tools: availableTools,
            ToolOutputs: []);

        WorkspaceArtifactDto? artifact = null;
        WorkspaceProposalDto? proposal = null;
        var toolAudit = new List<AgentToolCallDto>();
        var maxToolCalls = Math.Max(1, options.Value.MaxToolCallsPerTurn);
        string? finalMessage = null;

        for (var turn = 0; turn <= maxToolCalls; turn++)
        {
            var llmResponse = await llmClient.CreateResponseAsync(
                currentRequest,
                async (delta, progressCancellationToken) =>
                {
                    finalMessage += delta;

                    if (onProgress is null)
                    {
                        return;
                    }

                    await onProgress(new AgentStreamEventDto
                    {
                        Type = "assistant_message_delta",
                        OccurredAt = DateTimeOffset.UtcNow,
                        Message = delta
                    }, progressCancellationToken);
                },
                cancellationToken);
            finalMessage = llmResponse.AssistantMessage;

            if (llmResponse.ToolCalls.Count == 0)
            {
                break;
            }

            if (toolAudit.Count + llmResponse.ToolCalls.Count > maxToolCalls)
            {
                finalMessage = "I hit the tool-call limit for this turn. Please narrow the request or split it into smaller steps.";
                break;
            }

            var toolOutputs = new List<AgentFunctionCallOutput>(llmResponse.ToolCalls.Count);
            foreach (var toolCall in llmResponse.ToolCalls)
            {
                var arguments = ParseArguments(toolCall.ArgumentsJson);
                logger.LogInformation(
                    "Agent thread {ThreadId} selected tool {ToolName}",
                    context.ThreadId,
                    toolCall.Name);
                var startedAt = DateTimeOffset.UtcNow;
                var startedToolCall = new AgentToolCallDto
                {
                    Id = toolCall.CallId,
                    Name = toolCall.Name,
                    ArgumentsJson = arguments.ToJsonString(),
                    Status = "running",
                    StartedAt = startedAt
                };

                if (onProgress is not null)
                {
                    await onProgress(new AgentStreamEventDto
                    {
                        Type = "tool_call_started",
                        OccurredAt = startedAt,
                        ToolCall = startedToolCall
                    }, cancellationToken);
                }

                var toolResult = await backend.ExecuteModelToolAsync(
                    toolCall.Name,
                    arguments,
                    new AgentMcpCallContext(
                        tenantContext.TenantId,
                        tenantContext.ActorId,
                        Activity.Current?.Id ?? context.ThreadId),
                    cancellationToken);
                var completedAt = DateTimeOffset.UtcNow;

                logger.LogInformation(
                    "Agent thread {ThreadId} completed tool {ToolName} with status {Status}, payloadLength {PayloadLength}, artifactKind {ArtifactKind}, hasProposal {HasProposal}",
                    context.ThreadId,
                    toolCall.Name,
                    toolResult.Status,
                    toolResult.OutputJson.Length,
                    toolResult.Artifact?.Kind ?? "(none)",
                    toolResult.Proposal is not null);

                if (toolResult.Status == "completed" &&
                    toolResult.Artifact is null &&
                    toolResult.Proposal is null &&
                    toolCall.Name is "list_recordsets" or "resolve_record_schema" or "search_records" or "get_record" or "get_record_history")
                {
                    logger.LogWarning(
                        "Agent thread {ThreadId} completed structured tool {ToolName} without a mapped artifact or proposal",
                        context.ThreadId,
                        toolCall.Name);
                }

                if (toolResult.Status == "completed")
                {
                    artifact = toolResult.Artifact ?? artifact;
                    proposal = toolResult.Proposal ?? proposal;
                }

                var completedToolCall = new AgentToolCallDto
                {
                    Id = toolCall.CallId,
                    Name = toolCall.Name,
                    ArgumentsJson = arguments.ToJsonString(),
                    Status = toolResult.Status,
                    Summary = toolResult.Summary,
                    Error = toolResult.Error,
                    StartedAt = startedAt,
                    CompletedAt = completedAt
                };

                toolAudit.Add(completedToolCall);

                if (onProgress is not null)
                {
                    await onProgress(new AgentStreamEventDto
                    {
                        Type = toolResult.Status is "failed" or "rejected"
                            ? "tool_call_failed"
                            : "tool_call_completed",
                        OccurredAt = completedAt,
                        ToolCall = completedToolCall
                    }, cancellationToken);
                }

                toolOutputs.Add(new AgentFunctionCallOutput(toolCall.CallId, toolResult.ModelReceiptJson));
            }

            currentRequest = new AgentLlmRequest(
                Conversation: null,
                PreviousResponseId: llmResponse.ResponseId,
                Tools: availableTools,
                ToolOutputs: toolOutputs);
        }

        return new AgentTurnResultDto
        {
            AssistantMessage = string.IsNullOrWhiteSpace(finalMessage)
                ? toolAudit.Any(toolCall => toolCall.Status == "rejected")
                    ? "I couldn't complete that request with the tools available to this agent."
                    : "I completed the requested work."
                : finalMessage,
            Artifact = artifact,
            Proposal = proposal,
            ToolCalls = toolAudit.ToArray()
        };
    }

    private static JsonObject ParseArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(argumentsJson) as JsonObject ?? new JsonObject();
    }
}
