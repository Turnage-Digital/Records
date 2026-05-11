using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Records.Agents.Infrastructure.OpenAI;

namespace Records.Agents.Tests.Services;

public sealed class RecordsetsMcpClientTests
{
    [Test]
    public void ResolveTransportOptions_ShouldRequireExplicitCommand_WhenCommandIsMissing()
    {
        var client = CreateClient(
            new ConfigurationBuilder().Build(),
            new RecordsetsMcpOptions());

        Assert.That(
            () => client.ResolveTransportOptions(),
            Throws.InvalidOperationException.With.Message.Contains("Agents:Mcp:Recordsets:Command"));
    }

    [Test]
    public void ResolveTransportOptions_ShouldUseConfiguredCommandAndArguments_WhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "server=localhost;database=records"
            })
            .Build();
        var client = CreateClient(
            configuration,
            new RecordsetsMcpOptions
            {
                Command = "dotnet",
                Arguments = ["run", "--project", "../Modules/Recordsets/Records.Recordsets.McpServer/Records.Recordsets.McpServer.csproj"]
            });

        var options = client.ResolveTransportOptions();

        Assert.That(options.Command, Is.EqualTo("dotnet"));
        Assert.That(options.Arguments, Is.EqualTo(new[] { "run", "--project", "../Modules/Recordsets/Records.Recordsets.McpServer/Records.Recordsets.McpServer.csproj" }));
        Assert.That(options.WorkingDirectory, Is.EqualTo("/repo/src/Records.App.Server"));
    }

    [Test]
    public void SerializeToolResult_ShouldPreferNestedToolResultStructuredContent_WhenTopLevelStructuredContentIsMissing()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "schema": {
                "id": "01KMREA1DX5PNV7KMZR54GH5K4",
                "name": "Inventory",
                "columns": [],
                "statuses": []
              },
              "page": {
                "recordsetId": "01KMREA1DX5PNV7KMZR54GH5K4",
                "name": "Inventory",
                "count": 120,
                "items": []
              }
            }
            """);

        var result = new CallToolResult
        {
            Content =
            [
                new ToolResultContentBlock
                {
                    ToolUseId = "tool-search",
                    Content = [],
                    StructuredContent = document.RootElement.Clone()
                }
            ]
        };

        var serialized = RecordsetsMcpClient.SerializeToolResult(result);

        Assert.That(serialized, Does.Contain("\"schema\""));
        Assert.That(serialized, Does.Contain("\"page\""));
        Assert.That(serialized, Does.Not.Contain("\"structuredContent\""));
    }

    private static RecordsetsMcpClient CreateClient(IConfiguration configuration, RecordsetsMcpOptions options)
    {
        return new RecordsetsMcpClient(
            configuration,
            new FakeHostEnvironment(),
            Options.Create(options),
            NullLoggerFactory.Instance,
            NullLogger<RecordsetsMcpClient>.Instance);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Records.App.Server";
        public string ContentRootPath { get; set; } = "/repo/src/Records.App.Server";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
