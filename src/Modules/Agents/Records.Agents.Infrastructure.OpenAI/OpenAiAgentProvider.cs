using System.Diagnostics;
using System.Text.Json.Nodes;
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
    ITenantContext tenantContext
) : IAgentProvider
{
    public async Task<AgentTurnResultDto> ExecuteTurnAsync(
        AgentProviderContextDto context,
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
                context.CurrentArtifact is null ? null : AgentJsonSerializer.Serialize(context.CurrentArtifact),
                context.PendingProposal is null ? null : AgentJsonSerializer.Serialize(context.PendingProposal)),
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
            var llmResponse = await llmClient.CreateResponseAsync(currentRequest, cancellationToken);
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
                var startedAt = DateTimeOffset.UtcNow;
                var toolResult = await backend.ExecuteModelToolAsync(
                    toolCall.Name,
                    arguments,
                    new AgentMcpCallContext(
                        tenantContext.TenantId,
                        tenantContext.ActorId,
                        Activity.Current?.Id ?? context.ThreadId),
                    cancellationToken);
                var completedAt = DateTimeOffset.UtcNow;

                if (toolResult.Status == "completed")
                {
                    artifact = toolResult.Artifact ?? artifact;
                    proposal = toolResult.Proposal ?? proposal;
                }

                toolAudit.Add(new AgentToolCallDto
                {
                    Id = toolCall.CallId,
                    Name = toolCall.Name,
                    ArgumentsJson = arguments.ToJsonString(),
                    Status = toolResult.Status,
                    Summary = toolResult.Summary,
                    Error = toolResult.Error,
                    StartedAt = startedAt,
                    CompletedAt = completedAt
                });

                toolOutputs.Add(new AgentFunctionCallOutput(toolCall.CallId, toolResult.OutputJson));
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
