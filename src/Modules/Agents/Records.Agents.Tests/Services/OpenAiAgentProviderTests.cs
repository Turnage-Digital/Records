using System.Text.Json.Nodes;
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
        }, CancellationToken.None);

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
        }, CancellationToken.None);

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
        }, CancellationToken.None);

        Assert.That(result.AssistantMessage, Does.Contain("can't apply changes directly"));
        Assert.That(result.ToolCalls.Length, Is.EqualTo(1));
        Assert.That(result.ToolCalls[0].Name, Is.EqualTo("apply_record_update"));
        Assert.That(result.ToolCalls[0].Status, Is.EqualTo("rejected"));
        Assert.That(backendClient.CallCount, Is.Zero);
    }

    private static OpenAiAgentProvider CreateProvider(
        FakeRecordsetsMcpClient backendClient,
        IEnumerable<AgentLlmResponse> responses
    )
    {
        var backendRegistry = new AgentBackendRegistry(
        [
            new RecordsetsAgentBackend(backendClient)
        ]);

        return new OpenAiAgentProvider(
            new FakeAgentLlmClient(responses),
            backendRegistry,
            Options.Create(new AgentLlmOptions
            {
                Provider = "OpenAI",
                Model = "gpt-5-mini",
                MaxToolCallsPerTurn = 4
            }),
            new FakeTenantContext(UlidId.NewUlid().ToString(), UlidId.NewUlid().ToString()));
    }

    private sealed class FakeAgentLlmClient(IEnumerable<AgentLlmResponse> responses) : IAgentLlmClient
    {
        private readonly Queue<AgentLlmResponse> _responses = new(responses);

        public Task<AgentLlmResponse> CreateResponseAsync(AgentLlmRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responses.Dequeue());
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
