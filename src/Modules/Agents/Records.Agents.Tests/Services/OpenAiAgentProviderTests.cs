using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.OpenAI;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;

namespace Records.Agents.Tests.Services;

public sealed class OpenAiAgentProviderTests
{
    [Test]
    public async Task ExecuteTurnAsync_ShouldMapGridArtifactAndPersistToolAudit_WhenSearchToolCompletes()
    {
        var backendClient = new FakeRecordsetsMcpClient();
        var provider = CreateProvider(
            backendClient,
            [
                new AgentLlmResponse(
                    "response-1",
                    string.Empty,
                    [new AgentFunctionCallRequest(
                        "call-1",
                        "search_records",
                        $"{{\"collectionId\":\"{FakeRecordsetsMcpClient.RecordsetId}\",\"page\":0,\"pageSize\":20}}")]),
                new AgentLlmResponse(
                    "response-2",
                    "I found 1 result in Orders.",
                    [])
            ]);

        var result = await provider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = UlidId.NewUlid().ToString(),
            BackendId = "records",
            Message = "show me orders"
        }, null, CancellationToken.None);

        Assert.That(result.AssistantMessage, Is.EqualTo("I found 1 result in Orders."));
        Assert.That(result.Artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(result.Artifact?.Grid?.Rows.Length, Is.EqualTo(1));
        Assert.That(result.ToolCalls.Select(toolCall => toolCall.Name), Is.EquivalentTo(new[] { "search_records" }));
        Assert.That(result.ToolCalls[0].Status, Is.EqualTo("completed"));
        Assert.That(backendClient.CallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteTurnAsync_ShouldMapProposal_WhenValidationToolCompletes()
    {
        var provider = CreateProvider(
            new FakeRecordsetsMcpClient(),
            [
                new AgentLlmResponse(
                    "response-1",
                    string.Empty,
                    [new AgentFunctionCallRequest(
                        "call-1",
                        "validate_record_update",
                        $"{{\"collectionId\":\"{FakeRecordsetsMcpClient.RecordsetId}\",\"entityId\":101,\"changes\":{{\"status\":\"Complete\"}}}}")]),
                new AgentLlmResponse(
                    "response-2",
                    "I prepared a proposal with 1 change. Review it before applying.",
                    [])
            ]);

        var result = await provider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = UlidId.NewUlid().ToString(),
            BackendId = "records",
            Message = "update order 101 status to Complete"
        }, null, CancellationToken.None);

        Assert.That(result.Proposal, Is.Not.Null);
        Assert.That(result.Proposal!.Kind, Is.EqualTo("update"));
        Assert.That(result.Proposal.Diffs.Length, Is.EqualTo(1));
        Assert.That(result.ToolCalls[0].Name, Is.EqualTo("validate_record_update"));
        Assert.That(result.ToolCalls[0].Status, Is.EqualTo("completed"));
    }

    [Test]
    public async Task ExecuteTurnAsync_ShouldRejectHiddenMutationTool_WhenModelRequestsUnadvertisedApplyTool()
    {
        var backendClient = new FakeRecordsetsMcpClient();
        var provider = CreateProvider(
            backendClient,
            [
                new AgentLlmResponse(
                    "response-1",
                    string.Empty,
                    [new AgentFunctionCallRequest(
                        "call-1",
                        "apply_record_update",
                        $"{{\"collectionId\":\"{FakeRecordsetsMcpClient.RecordsetId}\",\"entityId\":101,\"changes\":{{\"status\":\"Complete\"}}}}")]),
                new AgentLlmResponse(
                    "response-2",
                    "I can't apply changes directly. I can only prepare a proposal for confirmation.",
                    [])
            ]);

        var result = await provider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = UlidId.NewUlid().ToString(),
            BackendId = "records",
            Message = "just do it"
        }, null, CancellationToken.None);

        Assert.That(result.AssistantMessage, Does.Contain("can't apply changes directly"));
        Assert.That(result.ToolCalls.Length, Is.EqualTo(1));
        Assert.That(result.ToolCalls[0].Name, Is.EqualTo("apply_record_update"));
        Assert.That(result.ToolCalls[0].Status, Is.EqualTo("rejected"));
        Assert.That(backendClient.CallCount, Is.Zero);
    }

    [Test]
    public async Task ExecuteTurnAsync_ShouldPublishTextAndToolProgress_WhenProviderStreamsWork()
    {
        var progressEvents = new List<AgentStreamEventDto>();
        var provider = CreateProvider(
            new FakeRecordsetsMcpClient(),
            [
                new AgentLlmResponse(
                    "response-1",
                    string.Empty,
                    [new AgentFunctionCallRequest(
                        "call-1",
                        "search_records",
                        $"{{\"collectionId\":\"{FakeRecordsetsMcpClient.RecordsetId}\",\"page\":0,\"pageSize\":20}}")]),
                new AgentLlmResponse(
                    "response-2",
                    "I found 1 result in Orders.",
                    [])
            ],
            ["I found 1 result in Orders."]);

        await provider.ExecuteTurnAsync(
            new AgentProviderContextDto
            {
                ThreadId = UlidId.NewUlid().ToString(),
                BackendId = "records",
                Message = "show me orders"
            },
            (streamEvent, _) =>
            {
                progressEvents.Add(streamEvent);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.That(
            progressEvents.Any(streamEvent =>
                streamEvent.Type == "assistant_message_delta" &&
                streamEvent.Message == "I found 1 result in Orders."),
            Is.True);
        Assert.That(
            progressEvents.Any(streamEvent =>
                streamEvent.Type == "tool_call_started" &&
                streamEvent.ToolCall?.Status == "running"),
            Is.True);
        Assert.That(
            progressEvents.Any(streamEvent =>
                streamEvent.Type == "tool_call_completed" &&
                streamEvent.ToolCall?.Status == "completed"),
            Is.True);
    }

    [Test]
    public async Task ExecuteTurnAsync_ShouldSendCompactGridReceiptToModel_WhenSearchToolCompletes()
    {
        var llmClient = new FakeAgentLlmClient(
        [
            new AgentLlmResponse(
                "response-1",
                string.Empty,
                [new AgentFunctionCallRequest(
                    "call-1",
                    "search_records",
                    $"{{\"collectionId\":\"{FakeRecordsetsMcpClient.RecordsetId}\",\"page\":0,\"pageSize\":20}}")]),
            new AgentLlmResponse(
                "response-2",
                "Loaded Orders in the work surface.",
                [])
        ]);
        var provider = CreateProvider(new FakeRecordsetsMcpClient(), llmClient);

        await provider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = UlidId.NewUlid().ToString(),
            BackendId = "records",
            Message = "show me orders"
        }, null, CancellationToken.None);

        Assert.That(llmClient.Requests, Has.Count.EqualTo(2));
        var receiptJson = llmClient.Requests[1].ToolOutputs.Single().ReceiptJson;
        Assert.That(receiptJson, Does.Contain("\"artifactKind\":\"grid\""));
        Assert.That(receiptJson, Does.Contain("\"rowReferences\""));
        Assert.That(receiptJson, Does.Not.Contain("\"attributes\""));
    }

    [Test]
    public async Task ExecuteTurnAsync_ShouldSummarizeGridArtifactContext_WhenCurrentArtifactIsLarge()
    {
        var llmClient = new FakeAgentLlmClient(
        [
            new AgentLlmResponse(
                "response-1",
                "I can open one of those rows next.",
                [])
        ]);
        var provider = CreateProvider(new FakeRecordsetsMcpClient(), llmClient);

        await provider.ExecuteTurnAsync(new AgentProviderContextDto
        {
            ThreadId = UlidId.NewUlid().ToString(),
            BackendId = "records",
            Message = "open the first one",
            CurrentArtifact = new WorkspaceArtifactDto
            {
                Kind = "grid",
                Title = "Orders",
                Grid = new WorkspaceGridDto
                {
                    CollectionId = FakeRecordsetsMcpClient.RecordsetId.ToString(),
                    CollectionLabel = "Orders",
                    Page = 0,
                    PageSize = 20,
                    TotalCount = 120,
                    Columns =
                    [
                        new WorkspaceGridColumnDto { Key = "client", Label = "Client", Type = "text" },
                        new WorkspaceGridColumnDto { Key = "status", Label = "Status", Type = "text" }
                    ],
                    Rows =
                    [
                        new WorkspaceGridRowDto
                        {
                            EntityId = "101",
                            DisplayName = "Order 101",
                            Attributes =
                            [
                                new WorkspaceAttributeDto
                                {
                                    Key = "client",
                                    Label = "Client",
                                    Type = "text",
                                    Value = "Acme",
                                    DisplayValue = "Acme"
                                }
                            ]
                        }
                    ]
                }
            }
        }, null, CancellationToken.None);

        var artifactContextJson = llmClient.Requests[0].Conversation?.CurrentArtifactJson;
        Assert.That(artifactContextJson, Is.Not.Null);
        Assert.That(artifactContextJson, Does.Contain("\"rowReferences\""));
        Assert.That(artifactContextJson, Does.Contain("\"entityId\":\"101\""));
        Assert.That(artifactContextJson, Does.Not.Contain("\"attributes\""));
    }

    private static OpenAiAgentProvider CreateProvider(
        FakeRecordsetsMcpClient backendClient,
        IEnumerable<AgentLlmResponse> responses,
        IEnumerable<string>? streamedText = null
    )
    {
        return CreateProvider(
            backendClient,
            new FakeAgentLlmClient(responses, streamedText));
    }

    private static OpenAiAgentProvider CreateProvider(
        FakeRecordsetsMcpClient backendClient,
        FakeAgentLlmClient llmClient
    )
    {
        var backendRegistry = new AgentBackendRegistry(
        [
            new RecordsetsAgentBackend(backendClient)
        ]);

        return new OpenAiAgentProvider(
            llmClient,
            backendRegistry,
            Options.Create(new AgentLlmOptions
            {
                Provider = "OpenAI",
                Model = "gpt-5-mini",
                MaxToolCallsPerTurn = 4
            }),
            new FakeTenantContext(UlidId.NewUlid().ToString(), UlidId.NewUlid().ToString()),
            NullLogger<OpenAiAgentProvider>.Instance);
    }

    private sealed class FakeAgentLlmClient(
        IEnumerable<AgentLlmResponse> responses,
        IEnumerable<string>? streamedText = null) : IAgentLlmClient
    {
        private readonly Queue<AgentLlmResponse> _responses = new(responses);
        private readonly Queue<string> _streamedText = new(streamedText ?? []);
        public List<AgentLlmRequest> Requests { get; } = [];

        public Task<AgentLlmResponse> CreateResponseAsync(
            AgentLlmRequest request,
            Func<string, CancellationToken, Task>? onTextDelta,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (onTextDelta is not null && _streamedText.TryDequeue(out var nextDelta))
            {
                return PublishDeltaAndDequeueAsync(nextDelta, onTextDelta, cancellationToken);
            }

            return Task.FromResult(_responses.Dequeue());
        }

        private async Task<AgentLlmResponse> PublishDeltaAndDequeueAsync(
            string delta,
            Func<string, CancellationToken, Task> onTextDelta,
            CancellationToken cancellationToken)
        {
            await onTextDelta(delta, cancellationToken);
            return _responses.Dequeue();
        }
    }

    private sealed class FakeRecordsetsMcpClient : IRecordsetsMcpClient
    {
        public static UlidId RecordsetId { get; } = UlidId.NewUlid();
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<AgentMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AgentMcpToolDefinition>>(
            [
                new("search_records", "Search records", "{}"),
                new("validate_record_update", "Validate record update", "{}"),
                new("apply_record_update", "Apply record update", "{}")
            ]);
        }

        public Task<AgentMcpToolCallResult> CallToolAsync(
            string toolName,
            JsonObject arguments,
            AgentMcpCallContext callContext,
            CancellationToken cancellationToken
        )
        {
            CallCount++;

            return Task.FromResult(toolName switch
            {
                "search_records" => new AgentMcpToolCallResult(
                    AgentJsonSerializer.Serialize(new RecordsetSearchToolResultDto
                    {
                        Schema = CreateSchema(),
                        Page = new RecordsetPagedRecordsDto
                        {
                            RecordsetId = RecordsetId,
                            Name = "Orders",
                            Count = 1,
                            Items =
                            [
                                new RecordListItemDto
                                {
                                    Id = 101,
                                    RecordsetId = RecordsetId,
                                    Bag = new Dictionary<string, object?>
                                    {
                                        ["client"] = "Acme",
                                        ["status"] = "Open"
                                    }
                                }
                            ]
                        }
                    }),
                    false,
                    null),
                "validate_record_update" => new AgentMcpToolCallResult(
                    AgentJsonSerializer.Serialize(new RecordMutationProposalDto
                    {
                        Kind = "update",
                        ProposalId = UlidId.NewUlid().ToString(),
                        RecordsetId = RecordsetId,
                        RecordsetName = "Orders",
                        Schema = CreateSchema(),
                        Current = new RecordItemDetailsDto
                        {
                            Id = 101,
                            RecordsetId = RecordsetId,
                            Bag = new Dictionary<string, object?>
                            {
                                ["client"] = "Acme",
                                ["status"] = "Open"
                            }
                        },
                        Proposed = new RecordItemDetailsDto
                        {
                            Id = 101,
                            RecordsetId = RecordsetId,
                            Bag = new Dictionary<string, object?>
                            {
                                ["client"] = "Acme",
                                ["status"] = "Complete"
                            }
                        },
                        Diffs =
                        [
                            new RecordMutationDiffDto
                            {
                                Key = "status",
                                Label = "Status",
                                Before = "Open",
                                After = "Complete"
                            }
                        ],
                        Rationale = "Prepared from the requested field changes.",
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
                    }),
                    false,
                    null),
                _ => new AgentMcpToolCallResult("{\"error\":\"Unknown tool.\"}", true, "Unknown tool.")
            });
        }

        private static RecordsetItemDefinitionDto CreateSchema()
        {
            return new RecordsetItemDefinitionDto
            {
                Id = RecordsetId,
                Name = "Orders",
                Columns =
                [
                    new RecordsetColumnDto
                    {
                        Name = "Client",
                        Property = "client",
                        Type = Records.Recordsets.Domain.ColumnType.Text,
                        Required = true
                    }
                ],
                Statuses =
                [
                    new RecordsetStatusDto { Name = "Open", Color = "#ccc" },
                    new RecordsetStatusDto { Name = "Complete", Color = "#090" }
                ]
            };
        }
    }

    private sealed class FakeTenantContext(string? tenantId, string? actorId) : ITenantContext
    {
        public string? TenantId { get; } = tenantId;
        public string? ActorId { get; } = actorId;
    }
}
