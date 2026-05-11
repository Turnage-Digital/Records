using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.OpenAI;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts.Dtos;
using Records.Users.Domain;

namespace Records.App.Server.Tests;

public sealed class AgentsEndpointTests
{
    [Test]
    public async Task ListThreads_ShouldScopeThreadsByUserAndTenant_WhenSameUserWorksAcrossTenants()
    {
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(
            rootFactory,
            new FakeAgentLlmClient(),
            new FakeRecordsetsMcpClient());

        var userId = UlidId.NewUlid();
        var tenantA = UlidId.NewUlid();
        var tenantB = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantA);
        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantB);

        using var clientA = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantA);
        using var clientB = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantB);

        var createAResponse = await clientA.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Tenant A thread"
        });
        var createBResponse = await clientB.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Tenant B thread"
        });

        Assert.That(createAResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(createBResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var listA = await clientA.GetFromJsonAsync<IReadOnlyList<AgentThreadSummaryDto>>(
            "/api/agents/threads",
            EndpointTestSupport.JsonOptions);
        var listB = await clientB.GetFromJsonAsync<IReadOnlyList<AgentThreadSummaryDto>>(
            "/api/agents/threads",
            EndpointTestSupport.JsonOptions);

        Assert.That(listA, Is.Not.Null);
        Assert.That(listB, Is.Not.Null);
        Assert.That(listA!.Select(x => x.Title), Is.EquivalentTo(new[] { "Tenant A thread" }));
        Assert.That(listB!.Select(x => x.Title), Is.EquivalentTo(new[] { "Tenant B thread" }));
    }

    [Test]
    public async Task DeleteThread_ShouldRemoveThreadAndRelatedRows_WhenThreadBelongsToCurrentUser()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Disposable thread"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(createdThread, Is.Not.Null);

        var listRecordsetsResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "what recordsets do I have?"
        });
        listRecordsetsResponse.EnsureSuccessStatusCode();

        var createProposalResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread.Id}/turns", new
        {
            message = "create an order for Acme with status Open"
        });
        createProposalResponse.EnsureSuccessStatusCode();

        var proposalThread = await createProposalResponse.Content.ReadFromJsonAsync<AgentThreadDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(proposalThread, Is.Not.Null);
        Assert.That(proposalThread!.CurrentArtifact?.Kind, Is.EqualTo("grid"));
        Assert.That(proposalThread.PendingProposal, Is.Not.Null);

        var deleteResponse = await client.DeleteAsync($"/api/agents/threads/{createdThread.Id}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var list = await client.GetFromJsonAsync<IReadOnlyList<AgentThreadSummaryDto>>(
            "/api/agents/threads",
            EndpointTestSupport.JsonOptions);
        Assert.That(list, Is.Not.Null);
        Assert.That(list!, Is.Empty);

        var getResponse = await client.GetAsync($"/api/agents/threads/{createdThread.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();
        var threadId = createdThread.Id;
        var threadCount = await db.AgentThreads.AsNoTracking().CountAsync(x => x.Id == threadId);
        var projectionCount = await db.AgentThreadProjections.AsNoTracking().CountAsync(x => x.Id == threadId);
        var turnCount = await db.AgentTurns.AsNoTracking().CountAsync(x => x.ThreadId == threadId);
        var toolCallCount = await db.AgentToolCalls.AsNoTracking().CountAsync(x => x.ThreadId == threadId);
        var artifactCount = await db.AgentArtifacts.AsNoTracking().CountAsync(x => x.ThreadId == threadId);
        var proposalCount = await db.AgentProposals.AsNoTracking().CountAsync(x => x.ThreadId == threadId);

        Assert.Multiple(() =>
        {
            Assert.That(threadCount, Is.Zero);
            Assert.That(projectionCount, Is.Zero);
            Assert.That(turnCount, Is.Zero);
            Assert.That(toolCallCount, Is.Zero);
            Assert.That(artifactCount, Is.Zero);
            Assert.That(proposalCount, Is.Zero);
        });
    }

    [Test]
    public async Task ConfirmProposal_ShouldApplyChangesOnlyAfterExplicitConfirmation()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Orders follow-up"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(createdThread, Is.Not.Null);

        var searchResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "show me orders for Acme yesterday"
        });
        if (!searchResponse.IsSuccessStatusCode)
        {
            Assert.Fail(await searchResponse.Content.ReadAsStringAsync());
        }

        var searchThread = await searchResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(searchThread, Is.Not.Null);
        Assert.That(searchThread!.CurrentArtifact?.Kind, Is.EqualTo("grid"));
        Assert.That(searchThread.CurrentArtifact?.Grid?.Rows.Length, Is.EqualTo(1));
        Assert.That(searchThread.Turns.Last().ToolCalls.Select(toolCall => toolCall.Name), Is.EquivalentTo(new[] { "search_records" }));

        var proposalResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread.Id}/turns", new
        {
            message = "update order 101 status to Complete"
        });
        proposalResponse.EnsureSuccessStatusCode();

        var proposalThread = await proposalResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(proposalThread, Is.Not.Null);
        Assert.That(proposalThread!.PendingProposal, Is.Not.Null);
        Assert.That(proposalThread.Turns.Last().ToolCalls.Select(toolCall => toolCall.Name), Is.EquivalentTo(new[] { "validate_record_update" }));
        Assert.That(mcpClient.ApplyCount, Is.Zero);

        var confirmResponse = await client.PostAsync(
            $"/api/agents/threads/{createdThread.Id}/proposals/{proposalThread.PendingProposal!.ProposalId}/confirm",
            null);
        confirmResponse.EnsureSuccessStatusCode();

        var confirmedThread = await confirmResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(confirmedThread, Is.Not.Null);
        Assert.That(confirmedThread!.PendingProposal, Is.Null);
        Assert.That(mcpClient.ApplyCount, Is.EqualTo(1));

        var status = confirmedThread.CurrentArtifact?.Detail?.Attributes
            .Single(attribute => attribute.Key == "status")
            .Value
            ?.ToString();

        Assert.That(status, Is.EqualTo("Complete"));
    }

    [Test]
    public async Task PostTurn_ShouldReturnGridArtifact_WhenModelListsRecordsets()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Recordsets"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);

        var threadResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "what recordsets do I have?"
        });
        threadResponse.EnsureSuccessStatusCode();

        var thread = await threadResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.CurrentArtifact?.Kind, Is.EqualTo("grid"));
        Assert.That(thread.CurrentArtifact?.Grid?.CollectionLabel, Is.EqualTo("Recordsets"));
        Assert.That(thread.CurrentArtifact?.Grid?.Rows.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task PostTurn_ShouldReturnEditorArtifact_WhenModelResolvesSchema()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Schema"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);

        var threadResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "is client a string?"
        });
        threadResponse.EnsureSuccessStatusCode();

        var thread = await threadResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.CurrentArtifact?.Kind, Is.EqualTo("editor"));
        Assert.That(thread.CurrentArtifact?.Editor?.CollectionLabel, Is.EqualTo("Orders"));
        Assert.That(thread.CurrentArtifact?.Editor?.Fields.Any(field => field.Key == "client"), Is.True);
    }

    [Test]
    public async Task PostTurn_ShouldReturnGridArtifact_WhenModelLoadsStudentsFirstPage()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Students"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);

        var threadResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "show me the first 20 rows of Students"
        });
        threadResponse.EnsureSuccessStatusCode();

        var thread = await threadResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.CurrentArtifact?.Kind, Is.EqualTo("grid"));
        Assert.That(thread.CurrentArtifact?.Grid?.CollectionLabel, Is.EqualTo("Students"));
        Assert.That(thread.CurrentArtifact?.Grid?.Rows.Length, Is.EqualTo(20));
    }

    [Test]
    public async Task ConfirmCreateProposal_ShouldPersistOnlyAfterExplicitConfirmation()
    {
        var llmClient = new FakeAgentLlmClient();
        var mcpClient = new FakeRecordsetsMcpClient();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, llmClient, mcpClient);

        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);

        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test", tenantId: tenantId);

        var createResponse = await client.PostAsJsonAsync("/api/agents/threads", new
        {
            title = "Create order"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdThread = await createResponse.Content.ReadFromJsonAsync<AgentThreadSummaryDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(createdThread, Is.Not.Null);

        var proposalResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread!.Id}/turns", new
        {
            message = "create an order for Acme with status Open"
        });
        proposalResponse.EnsureSuccessStatusCode();

        var proposalThread = await proposalResponse.Content.ReadFromJsonAsync<AgentThreadDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(proposalThread, Is.Not.Null);
        Assert.That(proposalThread!.PendingProposal, Is.Not.Null);
        Assert.That(proposalThread.PendingProposal!.Kind, Is.EqualTo("create"));
        Assert.That(mcpClient.ApplyCount, Is.Zero);

        var confirmResponse = await client.PostAsync(
            $"/api/agents/threads/{createdThread.Id}/proposals/{proposalThread.PendingProposal.ProposalId}/confirm",
            null);
        confirmResponse.EnsureSuccessStatusCode();

        var confirmedThread = await confirmResponse.Content.ReadFromJsonAsync<AgentThreadDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(confirmedThread, Is.Not.Null);
        Assert.That(confirmedThread!.PendingProposal, Is.Null);
        Assert.That(mcpClient.ApplyCount, Is.EqualTo(1));
        Assert.That(mcpClient.CreatedEntityCount, Is.EqualTo(2));

        var clientValue = confirmedThread.CurrentArtifact?.Detail?.Attributes
            .Single(attribute => attribute.Key == "client")
            .Value
            ?.ToString();

        Assert.That(clientValue, Is.EqualTo("Acme"));
    }

    private static WebApplicationFactory<Program> CreateFactory(
        RecordsWebApplicationFactory rootFactory,
        IAgentLlmClient llmClient,
        IRecordsetsMcpClient mcpClient
    )
    {
        return rootFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAgentLlmClient>();
            services.RemoveAll<IRecordsetsMcpClient>();
            services.AddSingleton<IAgentLlmClient>(llmClient);
            services.AddSingleton<IRecordsetsMcpClient>(mcpClient);
        }));
    }

    private sealed class FakeAgentLlmClient : IAgentLlmClient
    {
        public Task<AgentLlmResponse> CreateResponseAsync(
            AgentLlmRequest request,
            Func<string, CancellationToken, Task>? onTextDelta,
            CancellationToken cancellationToken)
        {
            if (request.Conversation is not null)
            {
                var message = request.Conversation.Message;

                if (message.Contains("show me orders", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "search-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-search",
                            "search_records",
                            JsonSerializer.Serialize(new
                            {
                                collectionId = FakeRecordsetsMcpClient.RecordsetId.ToString(),
                                page = 0,
                                pageSize = 20,
                                filters = new[]
                                {
                                    new { field = "client", @operator = "contains", value = "Acme" }
                                }
                            }))]));
                }

                if (message.Contains("recordsets", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "recordsets-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-recordsets",
                            "list_recordsets",
                            "{}")]));
                }

                if (message.Contains("client a string", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "schema-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-schema",
                            "resolve_record_schema",
                            JsonSerializer.Serialize(new
                            {
                                collectionId = FakeRecordsetsMcpClient.RecordsetId.ToString()
                            }))]));
                }

                if (message.Contains("first 20 rows of students", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "students-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-students",
                            "search_records",
                            JsonSerializer.Serialize(new
                            {
                                collectionId = FakeRecordsetsMcpClient.StudentsRecordsetId.ToString(),
                                page = 0,
                                pageSize = 20
                            }))]));
                }

                if (message.Contains("update order 101", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "update-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-update",
                            "validate_record_update",
                            JsonSerializer.Serialize(new
                            {
                                collectionId = FakeRecordsetsMcpClient.RecordsetId.ToString(),
                                entityId = 101,
                                changes = new { status = "Complete" }
                            }))]));
                }

                if (message.Contains("create an order", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new AgentLlmResponse(
                        "create-response",
                        string.Empty,
                        [new AgentFunctionCallRequest(
                            "tool-create",
                            "validate_record_create",
                            JsonSerializer.Serialize(new
                            {
                                collectionId = FakeRecordsetsMcpClient.RecordsetId.ToString(),
                                changes = new { client = "Acme", status = "Open" }
                            }))]));
                }
            }

            if (request.PreviousResponseId == "search-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "search-final",
                    "I found 1 result in Orders.",
                    []));
            }

            if (request.PreviousResponseId == "recordsets-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "recordsets-final",
                    "Here are your recordsets.",
                    []));
            }

            if (request.PreviousResponseId == "schema-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "schema-final",
                    "The client field is text.",
                    []));
            }

            if (request.PreviousResponseId == "students-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "students-final",
                    "Loaded the first 20 Students rows in the work surface.",
                    []));
            }

            if (request.PreviousResponseId == "update-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "update-final",
                    "I prepared a proposal with 1 change. Review it before applying.",
                    []));
            }

            if (request.PreviousResponseId == "create-response")
            {
                return Task.FromResult(new AgentLlmResponse(
                    "create-final",
                    "I prepared a proposal with 2 changes. Review it before creating.",
                    []));
            }

            return Task.FromResult(new AgentLlmResponse(
                "fallback",
                "I could not resolve that request.",
                []));
        }
    }

    private sealed class FakeRecordsetsMcpClient : IRecordsetsMcpClient
    {
        private readonly Dictionary<int, Dictionary<string, object?>> _entities = new()
        {
            [101] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["client"] = "Acme",
                ["status"] = "Open"
            }
        };

        private int _nextEntityId = 101;

        public static UlidId RecordsetId { get; } = UlidId.NewUlid();
        public static UlidId StudentsRecordsetId { get; } = UlidId.NewUlid();
        public int ApplyCount { get; private set; }
        public int CreatedEntityCount => _entities.Count;

        public Task<IReadOnlyList<AgentMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AgentMcpToolDefinition>>(
            [
                new("list_recordsets", "List recordsets", "{}"),
                new("resolve_record_schema", "Resolve schema", "{}"),
                new("search_records", "Search records", "{}"),
                new("get_record", "Get record", "{}"),
                new("get_record_history", "Get history", "{}"),
                new("validate_record_create", "Validate create", "{}"),
                new("validate_record_update", "Validate update", "{}"),
                new("apply_record_create", "Apply create", "{}"),
                new("apply_record_update", "Apply update", "{}")
            ]);
        }

        public Task<AgentMcpToolCallResult> CallToolAsync(
            string toolName,
            JsonObject arguments,
            AgentMcpCallContext callContext,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(toolName switch
            {
                "list_recordsets" => Success<IReadOnlyList<RecordsetNameDto>>(
                [
                    new RecordsetNameDto
                    {
                        Id = RecordsetId,
                        Name = "Orders",
                        Count = _entities.Count
                    },
                    new RecordsetNameDto
                    {
                        Id = StudentsRecordsetId,
                        Name = "Students",
                        Count = 180
                    }
                ]),
                "resolve_record_schema" => Success(CreateSchema()),
                "search_records" => Search(arguments),
                "validate_record_update" => Success(BuildUpdateProposal(arguments)),
                "validate_record_create" => Success(BuildCreateProposal(arguments)),
                "apply_record_update" => ApplyUpdate(arguments),
                "apply_record_create" => ApplyCreate(arguments),
                _ => new AgentMcpToolCallResult("{\"error\":\"Unsupported tool.\"}", true, "Unsupported tool.")
            });
        }

        private AgentMcpToolCallResult Search(JsonObject arguments)
        {
            var collectionId = arguments["collectionId"]?.GetValue<string>();
            if (string.Equals(collectionId, StudentsRecordsetId.ToString(), StringComparison.Ordinal))
            {
                return Success(new RecordsetSearchToolResultDto
                {
                    Schema = CreateStudentsSchema(),
                    Page = new RecordsetPagedRecordsDto
                    {
                        RecordsetId = StudentsRecordsetId,
                        Name = "Students",
                        Count = 180,
                        Items = Enumerable.Range(1, 20)
                            .Select(index => new RecordListItemDto
                            {
                                Id = 200 - index,
                                RecordsetId = StudentsRecordsetId,
                                Bag = new Dictionary<string, object?>
                                {
                                    ["name"] = $"Student {index}",
                                    ["status"] = index % 2 == 0 ? "Active" : "Probation",
                                    ["gpa"] = 3.0m + (index / 100m)
                                }
                            })
                            .ToArray()
                    }
                });
            }

            return Success(new RecordsetSearchToolResultDto
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
                            Bag = _entities[101]
                        }
                    ]
                }
            });
        }

        private AgentMcpToolCallResult ApplyUpdate(JsonObject arguments)
        {
            ApplyCount++;
            var entityId = arguments["entityId"]?.GetValue<int>() ?? 0;
            var changes = arguments["changes"]?.AsObject() ?? new JsonObject();
            foreach (var change in changes)
            {
                _entities[entityId][change.Key] = ReadNodeValue(change.Value);
            }

            return Success(new RecordsetApplyToolResultDto
            {
                Schema = CreateSchema(),
                Detail = new RecordItemDetailsDto
                {
                    Id = entityId,
                    RecordsetId = RecordsetId,
                    Bag = _entities[entityId]
                }
            });
        }

        private AgentMcpToolCallResult ApplyCreate(JsonObject arguments)
        {
            ApplyCount++;
            var changes = arguments["changes"]?.AsObject() ?? new JsonObject();
            var entityId = ++_nextEntityId;
            _entities[entityId] = changes.ToDictionary(
                pair => pair.Key,
                pair => ReadNodeValue(pair.Value),
                StringComparer.OrdinalIgnoreCase);

            return Success(new RecordsetApplyToolResultDto
            {
                Schema = CreateSchema(),
                Detail = new RecordItemDetailsDto
                {
                    Id = entityId,
                    RecordsetId = RecordsetId,
                    Bag = _entities[entityId]
                }
            });
        }

        private RecordMutationProposalDto BuildUpdateProposal(JsonObject arguments)
        {
            var changes = arguments["changes"]?.AsObject() ?? new JsonObject();
            var currentBag = new Dictionary<string, object?>(_entities[101], StringComparer.OrdinalIgnoreCase);
            var proposedBag = new Dictionary<string, object?>(currentBag, StringComparer.OrdinalIgnoreCase);

            foreach (var change in changes)
            {
                proposedBag[change.Key] = ReadNodeValue(change.Value);
            }

            return new RecordMutationProposalDto
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
                    Bag = currentBag
                },
                Proposed = new RecordItemDetailsDto
                {
                    Id = 101,
                    RecordsetId = RecordsetId,
                    Bag = proposedBag
                },
                Diffs =
                [
                    new RecordMutationDiffDto
                    {
                        Key = "status",
                        Label = "Status",
                        Before = currentBag["status"],
                        After = proposedBag["status"]
                    }
                ],
                Rationale = "Prepared from the requested field changes.",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            };
        }

        private RecordMutationProposalDto BuildCreateProposal(JsonObject arguments)
        {
            var changes = arguments["changes"]?.AsObject() ?? new JsonObject();
            var proposedBag = changes.ToDictionary(
                pair => pair.Key,
                pair => ReadNodeValue(pair.Value),
                StringComparer.OrdinalIgnoreCase);

            return new RecordMutationProposalDto
            {
                Kind = "create",
                ProposalId = UlidId.NewUlid().ToString(),
                RecordsetId = RecordsetId,
                RecordsetName = "Orders",
                Schema = CreateSchema(),
                Current = new RecordItemDetailsDto
                {
                    RecordsetId = RecordsetId,
                    Bag = new Dictionary<string, object?>()
                },
                Proposed = new RecordItemDetailsDto
                {
                    RecordsetId = RecordsetId,
                    Bag = proposedBag
                },
                Diffs =
                [
                    new RecordMutationDiffDto
                    {
                        Key = "client",
                        Label = "Client",
                        Before = null,
                        After = proposedBag["client"]
                    },
                    new RecordMutationDiffDto
                    {
                        Key = "status",
                        Label = "Status",
                        Before = null,
                        After = proposedBag["status"]
                    }
                ],
                Rationale = "Prepared from the requested field values.",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            };
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

        private static RecordsetItemDefinitionDto CreateStudentsSchema()
        {
            return new RecordsetItemDefinitionDto
            {
                Id = StudentsRecordsetId,
                Name = "Students",
                Columns =
                [
                    new RecordsetColumnDto
                    {
                        Name = "Name",
                        Property = "name",
                        Type = Records.Recordsets.Domain.ColumnType.Text,
                        Required = true
                    },
                    new RecordsetColumnDto
                    {
                        Name = "GPA",
                        Property = "gpa",
                        Type = Records.Recordsets.Domain.ColumnType.Number,
                        Required = false
                    }
                ],
                Statuses =
                [
                    new RecordsetStatusDto { Name = "Active", Color = "#090" },
                    new RecordsetStatusDto { Name = "Probation", Color = "#c90" }
                ]
            };
        }

        private static AgentMcpToolCallResult Success<T>(T value)
        {
            var json = JsonSerializer.Serialize(value, SerializerOptions);
            return new AgentMcpToolCallResult(json, false, null);
        }

        private static object? ReadNodeValue(JsonNode? value)
        {
            return value switch
            {
                null => null,
                JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
                JsonValue jsonValue when jsonValue.TryGetValue<int>(out var number) => number,
                JsonValue jsonValue when jsonValue.TryGetValue<long>(out var longValue) => longValue,
                JsonValue jsonValue when jsonValue.TryGetValue<bool>(out var booleanValue) => booleanValue,
                _ => value.ToJsonString()
            };
        }

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new UlidIdJsonConverter());
            return options;
        }
    }
}
