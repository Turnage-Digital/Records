using System.Text.Json;
using System.Text.Json.Nodes;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Infrastructure.Sql;

namespace Records.Agents.Tests.Services;

public sealed class RecordsBackendAdapterTests
{
    [Test]
    public async Task SearchAsync_ShouldResolveStructuredFilters_WhenPromptContainsClientAndRelativeDate()
    {
        var server = new FakeModuleServer
        {
            Recordsets =
            [
                new JsonObject
                {
                    ["id"] = "orders",
                    ["name"] = "Orders"
                }
            ],
            Schema = new WorkspaceEditorSchemaDto
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
                        Key = "orderDate",
                        Label = "Order Date",
                        Type = "date",
                        Required = true
                    }
                ]
            },
            SearchResult = new WorkspaceGridDto
            {
                CollectionId = "orders",
                CollectionLabel = "Orders"
            }
        };

        var adapter = new RecordsBackendAdapter([server]);

        var result = await adapter.SearchAsync(new WorkspaceSearchRequestDto
        {
            Query = "orders for Acme yesterday"
        }, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(server.SearchInvocations, Is.EqualTo(1));
        Assert.That(server.LastSearchFilters, Has.Count.EqualTo(3));
        Assert.That(server.LastSearchFilters.Any(filter =>
            Equals(filter["field"], "client") &&
            Equals(filter["operator"], "contains") &&
            Equals(filter["value"], "Acme")), Is.True);
        Assert.That(server.LastSearchFilters.Count(filter => Equals(filter["field"], "orderDate")), Is.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_ShouldReturnNull_WhenPromptDoesNotIdentifyACollectionAndMultipleExist()
    {
        var server = new FakeModuleServer
        {
            Recordsets =
            [
                new JsonObject
                {
                    ["id"] = "orders",
                    ["name"] = "Orders"
                },
                new JsonObject
                {
                    ["id"] = "invoices",
                    ["name"] = "Invoices"
                }
            ],
            Schema = new WorkspaceEditorSchemaDto
            {
                CollectionId = "orders",
                CollectionLabel = "Orders"
            },
            SearchResult = new WorkspaceGridDto
            {
                CollectionId = "orders",
                CollectionLabel = "Orders"
            }
        };

        var adapter = new RecordsBackendAdapter([server]);

        var result = await adapter.SearchAsync(new WorkspaceSearchRequestDto
        {
            Query = "show me what changed yesterday"
        }, CancellationToken.None);

        Assert.That(result, Is.Null);
        Assert.That(server.SearchInvocations, Is.Zero);
    }

    private sealed class FakeModuleServer : IAgentModuleServer
    {
        public string ModuleId => "recordsets";
        public IReadOnlyList<AgentModuleToolDescriptor> Tools => [];
        public JsonArray Recordsets { get; init; } = [];
        public WorkspaceEditorSchemaDto? Schema { get; init; }
        public WorkspaceGridDto? SearchResult { get; init; }
        public int SearchInvocations { get; private set; }
        public List<Dictionary<string, object?>> LastSearchFilters { get; } = [];

        public Task<JsonNode?> InvokeAsync(
            string operation,
            IReadOnlyDictionary<string, object?> arguments,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(operation switch
            {
                "list_recordsets" => (JsonNode?)Recordsets.DeepClone(),
                "resolve_record_schema" => JsonSerializer.SerializeToNode(Schema),
                "search_records" => HandleSearch(arguments),
                _ => null
            });
        }

        private JsonNode? HandleSearch(IReadOnlyDictionary<string, object?> arguments)
        {
            SearchInvocations++;

            if (arguments.TryGetValue("filters", out var filters) &&
                filters is IEnumerable<Dictionary<string, object?>> clauses)
            {
                LastSearchFilters.Clear();
                LastSearchFilters.AddRange(clauses.Select(clause => new Dictionary<string, object?>(clause)));
            }

            return JsonSerializer.SerializeToNode(SearchResult);
        }
    }
}
