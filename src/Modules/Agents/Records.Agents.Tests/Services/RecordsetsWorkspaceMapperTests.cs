using Records.Agents.Infrastructure.OpenAI;
using Records.Agents.Infrastructure.Sql;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Tests.Services;

public sealed class RecordsetsWorkspaceMapperTests
{
    [Test]
    public void SummarizeToolResult_ShouldUnwrapTextContentEnvelope_WhenListRecordsetsReturnsContentBlocks()
    {
        var outputJson =
            """
            [
              {
                "type": "text",
                "text": "[{\"id\":\"01KNJ1H9XMXJAAAWCZKCB3Y0AF\",\"name\":\"Orders\"}]"
              }
            ]
            """;

        var summary = RecordsetsWorkspaceMapper.SummarizeToolResult("list_recordsets", outputJson);

        Assert.That(summary, Is.EqualTo("Found 1 recordset(s)."));
    }

    [Test]
    public void TryMapArtifact_ShouldCreateGridArtifact_WhenListRecordsetsCompletes()
    {
        RecordsetNameDto[] recordsets =
        [
            new RecordsetNameDto
            {
                Id = UlidId.NewUlid(),
                Name = "Inventory",
                Count = 120
            },
            new RecordsetNameDto
            {
                Id = UlidId.NewUlid(),
                Name = "Students",
                Count = 180
            }
        ];
        var outputJson = AgentJsonSerializer.Serialize(recordsets);

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("list_recordsets", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(artifact?.Grid?.CollectionLabel, Is.EqualTo("Recordsets"));
        Assert.That(artifact?.Grid?.Rows.Length, Is.EqualTo(2));
        Assert.That(artifact?.Grid?.Rows.All(row => row.AvailableActions.Length == 0), Is.True);
    }

    [Test]
    public void TryMapArtifact_ShouldCreateGridArtifact_WhenListRecordsetsPayloadIsWrappedInResultObject()
    {
        RecordsetNameDto[] recordsets =
        [
            new RecordsetNameDto
            {
                Id = UlidId.NewUlid(),
                Name = "Inventory",
                Count = 120
            },
            new RecordsetNameDto
            {
                Id = UlidId.NewUlid(),
                Name = "Students",
                Count = 180
            }
        ];
        var outputJson =
            $$"""
              {
                "result": {{AgentJsonSerializer.Serialize(recordsets)}}
              }
              """;

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("list_recordsets", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(artifact?.Grid?.CollectionLabel, Is.EqualTo("Recordsets"));
        Assert.That(artifact?.Grid?.Rows.Length, Is.EqualTo(2));
    }

    [Test]
    public void TryMapArtifact_ShouldCreateEditorArtifact_WhenResolveRecordSchemaCompletes()
    {
        var outputJson = AgentJsonSerializer.Serialize(new RecordsetItemDefinitionDto
        {
            Id = UlidId.NewUlid(),
            Name = "Inventory",
            Columns =
            [
                new RecordsetColumnDto
                {
                    Name = "Assigned To",
                    Property = "assignedTo",
                    Type = ColumnType.Text,
                    Required = false
                }
            ],
            Statuses =
            [
                new RecordsetStatusDto
                {
                    Name = "Available",
                    Color = "#0f0"
                }
            ]
        });

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("resolve_record_schema", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("editor"));
        Assert.That(artifact?.Editor?.CollectionLabel, Is.EqualTo("Inventory"));
        Assert.That(
            artifact?.Editor?.Fields.Any(field => field.Key == "assignedTo" && field.Type == "text"),
            Is.True);
    }

    [Test]
    public void TryMapArtifact_ShouldReturnNull_WhenSearchResultIsEmptyObject()
    {
        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("search_records", "{}");

        Assert.That(artifact, Is.Null);
    }

    [Test]
    public void TryMapArtifact_ShouldCreateGridArtifact_WhenSearchResultPayloadIsWrappedInResultObject()
    {
        var recordsetId = UlidId.NewUlid();
        var outputJson =
            $$"""
              {
                "result": {{AgentJsonSerializer.Serialize(new RecordsetSearchToolResultDto
                {
                    Schema = new RecordsetItemDefinitionDto
                    {
                        Id = recordsetId,
                        Name = "Students",
                        Columns =
                        [
                            new RecordsetColumnDto
                            {
                                Name = "Name",
                                Property = "name",
                                Type = ColumnType.Text,
                                Required = true
                            }
                        ],
                        Statuses =
                        [
                            new RecordsetStatusDto
                            {
                                Name = "Active",
                                Color = "#0f0"
                            }
                        ]
                    },
                    Page = new RecordsetPagedRecordsDto
                    {
                        RecordsetId = recordsetId,
                        Name = "Students",
                        Count = 180,
                        Items =
                        [
                            new RecordListItemDto
                            {
                                Id = 180,
                                RecordsetId = recordsetId,
                                Bag = new Dictionary<string, object?>
                                {
                                    ["name"] = "Blake Young"
                                }
                            }
                        ]
                    }
                })}}
              }
              """;

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("search_records", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(artifact?.Grid?.CollectionLabel, Is.EqualTo("Students"));
        Assert.That(artifact?.Grid?.Rows.Length, Is.EqualTo(1));
    }

    [Test]
    public void TryMapArtifact_ShouldPreferStructuredContentOverTextFallback_WhenSearchResultContainsBoth()
    {
        var recordsetId = UlidId.NewUlid();
        var structuredResult = AgentJsonSerializer.Serialize(new RecordsetSearchToolResultDto
        {
            Schema = new RecordsetItemDefinitionDto
            {
                Id = recordsetId,
                Name = "Students",
                Columns =
                [
                    new RecordsetColumnDto
                    {
                        Name = "Name",
                        Property = "name",
                        Type = ColumnType.Text,
                        Required = true
                    }
                ],
                Statuses =
                [
                    new RecordsetStatusDto
                    {
                        Name = "Active",
                        Color = "#0f0"
                    }
                ]
            },
            Page = new RecordsetPagedRecordsDto
            {
                RecordsetId = recordsetId,
                Name = "Students",
                Count = 180,
                Items =
                [
                    new RecordListItemDto
                    {
                        Id = 180,
                        RecordsetId = recordsetId,
                        Bag = new Dictionary<string, object?>
                        {
                            ["name"] = "Blake Young"
                        }
                    }
                ]
            }
        });
        var outputJson =
            $$"""
              [
                {
                  "type": "text",
                  "text": "Loaded the first 20 students."
                },
                {
                  "type": "tool_result",
                  "structuredContent": {{structuredResult}}
                }
              ]
              """;

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("search_records", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(artifact?.Grid?.CollectionLabel, Is.EqualTo("Students"));
        Assert.That(artifact?.Grid?.Rows.Length, Is.EqualTo(1));
    }

    [Test]
    public void TryMapArtifact_ShouldCreateGridArtifact_WhenSearchResultMatchesLiveMcpPayloadShape()
    {
        var outputJson =
            """
            {
              "schema": {
                "id": "01KMREA1DX5PNV7KMZR54GH5K4",
                "name": "Inventory",
                "columns": [
                  {
                    "key": "assetTag",
                    "name": "Asset Tag",
                    "property": "assetTag",
                    "type": "Text",
                    "required": true,
                    "regex": "^AST-\\d{4}$"
                  },
                  {
                    "key": "assignedTo",
                    "name": "Assigned To",
                    "property": "assignedTo",
                    "type": "Text",
                    "required": false
                  }
                ],
                "statuses": [
                  {
                    "name": "Available",
                    "color": "#8BC34A"
                  },
                  {
                    "name": "In Use",
                    "color": "#29B6F6"
                  }
                ],
                "transitions": [
                  {
                    "from": "Available",
                    "allowedNext": [
                      "In Use"
                    ]
                  }
                ]
              },
              "page": {
                "id": "01KMREA1DX5PNV7KMZR54GH5K4",
                "name": "Inventory",
                "count": 120,
                "items": [
                  {
                    "id": 376,
                    "recordsetId": "01KMREA1DX5PNV7KMZR54GH5K4",
                    "bag": {
                      "status": "Available",
                      "assetTag": "AST-1099",
                      "assignedTo": "Noah Kelly"
                    }
                  }
                ]
              }
            }
            """;

        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("search_records", outputJson);

        Assert.That(artifact?.Kind, Is.EqualTo("grid"));
        Assert.That(artifact?.Grid?.CollectionLabel, Is.EqualTo("Inventory"));
        Assert.That(artifact?.Grid?.CollectionId, Is.EqualTo("01KMREA1DX5PNV7KMZR54GH5K4"));
        Assert.That(artifact?.Grid?.Rows.Length, Is.EqualTo(1));
        Assert.That(artifact?.Grid?.Rows[0].DisplayName, Is.EqualTo("Inventory #376"));
    }

    [Test]
    public void TryMapArtifact_ShouldReturnNull_WhenSchemaResultIsEmptyObject()
    {
        var artifact = RecordsetsWorkspaceMapper.TryMapArtifact("resolve_record_schema", "{}");

        Assert.That(artifact, Is.Null);
    }
}
