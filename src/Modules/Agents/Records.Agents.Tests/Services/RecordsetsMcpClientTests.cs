using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
