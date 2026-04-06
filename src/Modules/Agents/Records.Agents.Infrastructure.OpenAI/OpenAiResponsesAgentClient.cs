#pragma warning disable OPENAI001

using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class OpenAiResponsesAgentClient(IOptions<AgentLlmOptions> options) : IAgentLlmClient
{
    public async Task<AgentLlmResponse> CreateResponseAsync(AgentLlmRequest request, CancellationToken cancellationToken)
    {
        var currentOptions = options.Value;
        if (!string.Equals(currentOptions.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentLlmResponse(
                null,
                $"Unsupported LLM provider '{currentOptions.Provider}'. Configure Agents:Llm:Provider as OpenAI.",
                []);
        }

        if (string.IsNullOrWhiteSpace(currentOptions.ApiKey))
        {
            return new AgentLlmResponse(
                null,
                "Agents LLM is not configured. Set Agents:Llm:ApiKey and Agents:Llm:Model to enable real tool-driven responses.",
                []);
        }

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(currentOptions.BaseUrl))
        {
            clientOptions.Endpoint = new Uri(currentOptions.BaseUrl);
        }

        var client = new ResponsesClient(new ApiKeyCredential(currentOptions.ApiKey), clientOptions);
        var responseOptions = new CreateResponseOptions
        {
            Model = currentOptions.Model,
            Instructions = BuildInstructions(),
            PreviousResponseId = request.PreviousResponseId,
            ParallelToolCallsEnabled = false,
            MaxToolCallCount = currentOptions.MaxToolCallsPerTurn
        };

        foreach (var tool in request.Tools)
        {
            responseOptions.Tools.Add(
                ResponseTool.CreateFunctionTool(
                    tool.Name,
                    BinaryData.FromString(tool.JsonSchema),
                    strictModeEnabled: false,
                    functionDescription: tool.Description));
        }

        if (request.Conversation is not null)
        {
            responseOptions.InputItems.Add(
                ResponseItem.CreateUserMessageItem(BuildConversationInput(request.Conversation)));
        }

        foreach (var toolOutput in request.ToolOutputs)
        {
            responseOptions.InputItems.Add(
                ResponseItem.CreateFunctionCallOutputItem(toolOutput.CallId, toolOutput.OutputJson));
        }

        var response = (await client.CreateResponseAsync(responseOptions, cancellationToken)).Value;
        var toolCalls = response.OutputItems
            .OfType<FunctionCallResponseItem>()
            .Select(item => new AgentFunctionCallRequest(
                item.CallId,
                item.FunctionName,
                item.FunctionArguments.ToString()))
            .ToArray();

        return new AgentLlmResponse(
            response.Id,
            response.GetOutputText(),
            toolCalls);
    }

    private static string BuildInstructions()
    {
        return """
               You are the Records app operations assistant.
               Use the available tools to inspect recordsets, inspect records, load history, and prepare create or update proposals.
               Never claim that a create or update has been applied unless the user explicitly confirms a proposal through the app.
               For create and update requests, use validate_record_create or validate_record_update after you have enough context.
               For read requests, prefer search_records, get_record, and get_record_history.
               Keep the final answer short, concrete, and grounded in the tool results.
               If a tool result is missing or empty, explain what is missing rather than inventing data.
               """;
    }

    private static string BuildConversationInput(AgentLlmConversationContext conversation)
    {
        return $$"""
                 Backend ID: {{conversation.BackendId}}

                 Thread ID: {{conversation.ThreadId}}

                 Current artifact JSON:
                 {{conversation.CurrentArtifactJson ?? "null"}}

                 Pending proposal JSON:
                 {{conversation.PendingProposalJson ?? "null"}}

                 User message:
                 {{conversation.Message}}

                 Pasted text:
                 {{conversation.PastedText ?? "(none)"}}
                 """;
    }
}
