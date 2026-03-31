using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.App.Server.Tests;

public sealed class AgentsEndpointTests
{
    [Test]
    public async Task ListThreads_ShouldScopeThreadsByUserAndTenant_WhenSameUserWorksAcrossTenants()
    {
        var adapter = new FakeWorkspaceBackendAdapter();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, adapter);

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
    public async Task ConfirmProposal_ShouldApplyChangesOnlyAfterExplicitConfirmation()
    {
        var adapter = new FakeWorkspaceBackendAdapter();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, adapter);

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

        var proposalResponse = await client.PostAsJsonAsync($"/api/agents/threads/{createdThread.Id}/turns", new
        {
            message = "update order 101 status to Complete"
        });
        proposalResponse.EnsureSuccessStatusCode();

        var proposalThread = await proposalResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(proposalThread, Is.Not.Null);
        Assert.That(proposalThread!.PendingProposal, Is.Not.Null);
        Assert.That(adapter.ApplyCount, Is.Zero);

        var confirmResponse = await client.PostAsync(
            $"/api/agents/threads/{createdThread.Id}/proposals/{proposalThread.PendingProposal!.ProposalId}/confirm",
            null);
        confirmResponse.EnsureSuccessStatusCode();

        var confirmedThread = await confirmResponse.Content.ReadFromJsonAsync<AgentThreadDto>(EndpointTestSupport.JsonOptions);
        Assert.That(confirmedThread, Is.Not.Null);
        Assert.That(confirmedThread!.PendingProposal, Is.Null);
        Assert.That(adapter.ApplyCount, Is.EqualTo(1));

        var status = confirmedThread.CurrentArtifact?.Detail?.Attributes
            .Single(attribute => attribute.Key == "status")
            .Value
            ?.ToString();

        Assert.That(status, Is.EqualTo("Complete"));
    }

    [Test]
    public async Task ConfirmCreateProposal_ShouldPersistOnlyAfterExplicitConfirmation()
    {
        var adapter = new FakeWorkspaceBackendAdapter();
        await using var rootFactory = new RecordsWebApplicationFactory();
        await using var factory = CreateFactory(rootFactory, adapter);

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
        Assert.That(adapter.ApplyCount, Is.Zero);

        var confirmResponse = await client.PostAsync(
            $"/api/agents/threads/{createdThread.Id}/proposals/{proposalThread.PendingProposal.ProposalId}/confirm",
            null);
        confirmResponse.EnsureSuccessStatusCode();

        var confirmedThread = await confirmResponse.Content.ReadFromJsonAsync<AgentThreadDto>(
            EndpointTestSupport.JsonOptions);
        Assert.That(confirmedThread, Is.Not.Null);
        Assert.That(confirmedThread!.PendingProposal, Is.Null);
        Assert.That(adapter.ApplyCount, Is.EqualTo(1));
        Assert.That(adapter.CreatedEntityCount, Is.EqualTo(2));

        var clientValue = confirmedThread.CurrentArtifact?.Detail?.Attributes
            .Single(attribute => attribute.Key == "client")
            .Value
            ?.ToString();

        Assert.That(clientValue, Is.EqualTo("Acme"));
    }

    private static WebApplicationFactory<Program> CreateFactory(
        RecordsWebApplicationFactory rootFactory,
        FakeWorkspaceBackendAdapter adapter
    )
    {
        return rootFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWorkspaceBackendAdapter>();
            services.AddSingleton<IWorkspaceBackendAdapter>(adapter);
        }));
    }

    private sealed class FakeWorkspaceBackendAdapter : IWorkspaceBackendAdapter
    {
        private readonly Dictionary<string, WorkspaceEntityDto> _entities = new(StringComparer.Ordinal)
        {
            ["101"] = CreateEntity("101", "Open")
        };
        private int _nextEntityId = 101;

        public string Id => "records";
        public int ApplyCount { get; private set; }
        public int CreatedEntityCount => _entities.Count;

        public Task<WorkspaceGridDto?> SearchAsync(
            WorkspaceSearchRequestDto request,
            CancellationToken cancellationToken
        )
        {
            var entity = _entities["101"];

            return Task.FromResult<WorkspaceGridDto?>(new WorkspaceGridDto
            {
                CollectionId = "orders",
                CollectionLabel = "Orders",
                ResolvedFilters = ["client contains Acme", "orderDate on_or_after yesterday"],
                Page = 0,
                PageSize = 20,
                TotalCount = 1,
                Columns =
                [
                    new WorkspaceGridColumnDto { Key = "client", Label = "Client", Type = "text" },
                    new WorkspaceGridColumnDto { Key = "status", Label = "Status", Type = "enum" }
                ],
                Rows =
                [
                    new WorkspaceGridRowDto
                    {
                        EntityId = "101",
                        DisplayName = "Order 101",
                        AvailableActions = ["inspect", "update"],
                        Attributes =
                        [
                            new WorkspaceAttributeDto
                            {
                                Key = "client",
                                Label = "Client",
                                Type = "text",
                                Value = "Acme"
                            },
                            new WorkspaceAttributeDto
                            {
                                Key = "status",
                                Label = "Status",
                                Type = "enum",
                                Value = entity.Attributes.Single(attribute => attribute.Key == "status").Value
                            }
                        ]
                    }
                ]
            });
        }

        public Task<WorkspaceEntityDto?> GetEntityAsync(
            WorkspaceEntityRequestDto request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(_entities.TryGetValue(request.EntityId, out var entity) ? entity : null);
        }

        public Task<WorkspaceHistoryDto?> GetHistoryAsync(
            WorkspaceEntityRequestDto request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<WorkspaceHistoryDto?>(new WorkspaceHistoryDto
            {
                EntityId = request.EntityId,
                Entries =
                [
                    new WorkspaceHistoryEntryDto
                    {
                        Type = "Updated",
                        OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-15),
                        ActorId = "ops-user",
                        Attributes =
                        [
                            new WorkspaceAttributeDto
                            {
                                Key = "status",
                                Label = "Status",
                                Type = "enum",
                                Value = _entities[request.EntityId].Attributes.Single(attribute => attribute.Key == "status").Value
                            }
                        ]
                    }
                ]
            });
        }

        public Task<WorkspaceEditorSchemaDto?> ResolveSchemaAsync(
            WorkspaceResolveSchemaRequestDto request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<WorkspaceEditorSchemaDto?>(CreateSchema());
        }

        public Task<WorkspaceProposalDto?> ProposeUpdateAsync(
            WorkspaceProposeUpdateRequestDto request,
            CancellationToken cancellationToken
        )
        {
            const string entityId = "101";
            var current = _entities[entityId];
            var nextStatus = request.Prompt.Contains("complete", StringComparison.OrdinalIgnoreCase)
                ? "Complete"
                : "Open";
            var proposed = CreateEntity(entityId, nextStatus);

            return Task.FromResult<WorkspaceProposalDto?>(new WorkspaceProposalDto
            {
                Kind = "update",
                ProposalId = UlidId.NewUlid().ToString(),
                Target = current,
                Current = current,
                Proposed = proposed,
                Rationale = "Prepared from the requested status change.",
                State = "pending",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                Diffs =
                [
                    new WorkspaceProposalDiffDto
                    {
                        Key = "status",
                        Label = "Status",
                        Before = current.Attributes.Single(x => x.Key == "status").Value,
                        After = nextStatus
                    }
                ]
            });
        }

        public Task<WorkspaceProposalDto?> ProposeCreateAsync(
            WorkspaceProposeCreateRequestDto request,
            CancellationToken cancellationToken
        )
        {
            var client = request.Prompt.Contains("Acme", StringComparison.OrdinalIgnoreCase)
                ? "Acme"
                : "New client";
            var status = request.Prompt.Contains("complete", StringComparison.OrdinalIgnoreCase)
                ? "Complete"
                : "Open";
            var proposed = CreateEntity("draft-order", status, client);

            return Task.FromResult<WorkspaceProposalDto?>(new WorkspaceProposalDto
            {
                Kind = "create",
                ProposalId = UlidId.NewUlid().ToString(),
                Target = proposed,
                Current = CreateEntity("draft-order", string.Empty, string.Empty),
                Proposed = proposed,
                Rationale = "Prepared a draft order from the requested field values.",
                State = "pending",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                Diffs =
                [
                    new WorkspaceProposalDiffDto
                    {
                        Key = "client",
                        Label = "Client",
                        Before = null,
                        After = client
                    },
                    new WorkspaceProposalDiffDto
                    {
                        Key = "status",
                        Label = "Status",
                        Before = null,
                        After = status
                    }
                ]
            });
        }

        public Task<WorkspaceEntityDto?> ApplyConfirmedProposalAsync(
            WorkspaceApplyProposalRequestDto request,
            CancellationToken cancellationToken
        )
        {
            ApplyCount++;

            if (string.Equals(request.Proposal.Kind, "create", StringComparison.OrdinalIgnoreCase))
            {
                var createdId = Interlocked.Increment(ref _nextEntityId).ToString();
                var status = request.Proposal.Diffs.SingleOrDefault(diff => diff.Key == "status")?.After?.ToString() ?? "Open";
                var client = request.Proposal.Diffs.SingleOrDefault(diff => diff.Key == "client")?.After?.ToString() ?? "New client";
                var created = CreateEntity(createdId, status, client);
                _entities[createdId] = created;
                return Task.FromResult<WorkspaceEntityDto?>(created);
            }

            var targetEntityId = request.Proposal.Target.EntityId;
            var current = _entities[targetEntityId];
            var statusDiff = request.Proposal.Diffs.Single(diff => diff.Key == "status");
            var nextStatus = statusDiff.After?.ToString() ?? current.Attributes.Single(attribute => attribute.Key == "status").Value?.ToString() ?? "Open";
            var updated = CreateEntity(targetEntityId, nextStatus);
            _entities[targetEntityId] = updated;
            return Task.FromResult<WorkspaceEntityDto?>(updated);
        }

        public Task<WorkspaceNotificationsDto?> GetContextNotificationsAsync(
            WorkspaceEntityRequestDto request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<WorkspaceNotificationsDto?>(new WorkspaceNotificationsDto
            {
                EntityId = request.EntityId,
                UnreadCount = 1,
                Items =
                [
                    new WorkspaceNotificationDto
                    {
                        Id = "notification-1",
                        Title = "Order updated",
                        Body = "Operations requested a follow-up on this order.",
                        OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                    }
                ]
            });
        }

        private static WorkspaceEditorSchemaDto CreateSchema()
        {
            return new WorkspaceEditorSchemaDto
            {
                CollectionId = "orders",
                CollectionLabel = "Orders",
                Fields =
                [
                    new WorkspaceSchemaFieldDto
                    {
                        Key = "client",
                        Label = "Client",
                        Type = "text",
                        Required = true
                    },
                    new WorkspaceSchemaFieldDto
                    {
                        Key = "status",
                        Label = "Status",
                        Type = "enum",
                        Required = true,
                        AllowedValues = ["Open", "Complete"]
                    }
                ],
                StateTransitions =
                [
                    new WorkspaceStateTransitionDto
                    {
                        From = "Open",
                        AllowedNext = ["Complete"]
                    }
                ]
            };
        }

        private static WorkspaceEntityDto CreateEntity(
            string entityId,
            string status,
            string client = "Acme"
        )
        {
            var schema = CreateSchema();
            return new WorkspaceEntityDto
            {
                EntityType = "order",
                EntityId = entityId,
                CollectionId = "orders",
                DisplayName = $"Order {entityId}",
                Schema = schema,
                Attributes =
                [
                    new WorkspaceAttributeDto
                    {
                        Key = "client",
                        Label = "Client",
                        Type = "text",
                        Value = client
                    },
                    new WorkspaceAttributeDto
                    {
                        Key = "status",
                        Label = "Status",
                        Type = "enum",
                        Value = status
                    }
                ]
            };
        }
    }
}
