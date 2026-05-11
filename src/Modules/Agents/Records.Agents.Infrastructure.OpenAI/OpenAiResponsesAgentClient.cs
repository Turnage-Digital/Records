#pragma warning disable OPENAI001

using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class OpenAiResponsesAgentClient(IOptions<AgentLlmOptions> options) : IAgentLlmClient
{
    public async Task<AgentLlmResponse> CreateResponseAsync(
        AgentLlmRequest request,
        Func<string, CancellationToken, Task>? onTextDelta,
        CancellationToken cancellationToken)
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
            StreamingEnabled = true,
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
                ResponseItem.CreateFunctionCallOutputItem(toolOutput.CallId, toolOutput.ReceiptJson));
        }

        string? responseId = null;
        string finalMessage = string.Empty;
        ResponseResult? completedResponse = null;
        var toolCalls = new List<AgentFunctionCallRequest>();

        await foreach (var update in client
                           .CreateResponseStreamingAsync(responseOptions, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (update is StreamingResponseCreatedUpdate createdUpdate)
            {
                responseId ??= createdUpdate.Response.Id;
                continue;
            }

            if (update is StreamingResponseOutputTextDeltaUpdate textDeltaUpdate)
            {
                finalMessage += textDeltaUpdate.Delta;

                if (onTextDelta is not null && !string.IsNullOrEmpty(textDeltaUpdate.Delta))
                {
                    await onTextDelta(textDeltaUpdate.Delta, cancellationToken);
                }

                continue;
            }

            if (update is StreamingResponseCompletedUpdate completedUpdate)
            {
                completedResponse = completedUpdate.Response;
                responseId ??= completedResponse.Id;
            }
        }

        if (completedResponse is not null)
        {
            if (string.IsNullOrWhiteSpace(finalMessage))
            {
                finalMessage = completedResponse.GetOutputText();
            }

            if (toolCalls.Count == 0)
            {
                toolCalls.AddRange(completedResponse.OutputItems
                    .OfType<FunctionCallResponseItem>()
                    .Select(item => new AgentFunctionCallRequest(
                        item.CallId,
                        item.FunctionName,
                        item.FunctionArguments.ToString())));
            }
        }

        return new AgentLlmResponse(
            responseId,
            finalMessage,
            toolCalls);
    }

    private static string BuildInstructions()
    {
        return """
               You are the Records app operations assistant for this application, not a general-purpose assistant.
               Stay focused on Records workflows: recordsets, records, schema, history, and draft create/update changes.
               If the user asks for something unrelated to the Records app or the current Records context, say briefly that you can only help with Records app work here.
               Use the available tools to inspect recordsets, inspect records, load history, and prepare create or update proposals.
               Use tools whenever a Records answer depends on current app data. Do not answer Records facts from general model knowledge or guess.
               Never claim that a create or update has been applied unless the user explicitly confirms a proposal through the app.
               For create and update requests, use validate_record_create or validate_record_update after you have enough context.
               For read requests, prefer search_records, get_record, and get_record_history.
               When the user asks to open a recordset or collection, resolve it and call search_records with page = 0, pageSize = 20, and filters = [] so the structured work surface shows the first page of the record list.
               When the user asks about a recordset schema or field types, call resolve_record_schema so the structured work surface shows the schema.
               When you need a recordset by name, call list_recordsets first and use the returned id as search_records.collectionId or resolve_record_schema.collectionId.
               For requests like "load the first 20 records from Inventory", call search_records with the Inventory collectionId, page = 0, pageSize = 20, and filters = [].
               For requests like "load 1 record from Inventory" when no entity id is known yet, call search_records with page = 0, pageSize = 1, and filters = [].
               When a tool produces a structured work surface result such as a list, grid, schema, or detail view, keep the final answer brief and point to the work surface instead of restating the entire dataset in chat.
               Keep the final answer short, concrete, and grounded in the tool results.
               If a tool result is missing or empty, explain what is missing rather than inventing data.
               """;
    }

    private static string BuildConversationInput(AgentLlmConversationContext conversation)
    {
        return $$"""
                 Backend ID: {{conversation.BackendId}}

                 Thread ID: {{conversation.ThreadId}}

                 Current artifact context JSON:
                 {{conversation.CurrentArtifactJson ?? "null"}}

                 Pending proposal context JSON:
                 {{conversation.PendingProposalJson ?? "null"}}

                 User message:
                 {{conversation.Message}}

                 Pasted text:
                 {{conversation.PastedText ?? "(none)"}}
                 """;
    }
}
