using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Records.Agents.Infrastructure.Sql;

namespace Records.Agents.Infrastructure.OpenAI;

internal sealed class RecordsetsMcpClient(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IOptions<RecordsetsMcpOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<RecordsetsMcpClient> logger
) : IRecordsetsMcpClient, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _client;
    private StdioClientTransport? _transport;

    public async Task<IReadOnlyList<AgentMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken);

        try
        {
            var result = await client.ListToolsAsync(new RequestOptions(), cancellationToken);
            return result
                .Select(tool => new AgentMcpToolDefinition(
                    tool.Name,
                    tool.Description ?? string.Empty,
                    tool.JsonSchema.GetRawText()))
                .ToArray();
        }
        catch
        {
            await ResetClientAsync();
            throw;
        }
    }

    public async Task<AgentMcpToolCallResult> CallToolAsync(
        string toolName,
        JsonObject arguments,
        AgentMcpCallContext callContext,
        CancellationToken cancellationToken
    )
    {
        using var timeoutScope = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutScope.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, options.Value.CallTimeoutSeconds)));

        var client = await GetClientAsync(timeoutScope.Token);

        try
        {
            var requestOptions = new RequestOptions
            {
                Meta = CreateMeta(callContext),
                JsonSerializerOptions = AgentJsonSerializer.Options
            };

            var result = await client.CallToolAsync(
                toolName,
                ToArgumentsDictionary(arguments),
                progress: null,
                requestOptions,
                timeoutScope.Token);

            var outputJson = SerializeToolResult(result);
            return new AgentMcpToolCallResult(
                outputJson,
                result.IsError == true,
                result.IsError == true ? outputJson : null);
        }
        catch (Exception exception)
        {
            await ResetClientAsync();
            logger.LogWarning(exception, "MCP tool call failed for {ToolName}", toolName);
            var errorJson = new JsonObject
            {
                ["error"] = exception.Message
            }.ToJsonString();
            return new AgentMcpToolCallResult(errorJson, true, exception.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetClientAsync();
        _gate.Dispose();
    }

    private async Task<McpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null)
            {
                return _client;
            }

            var resolvedOptions = ResolveTransportOptions();
            _transport = new StdioClientTransport(resolvedOptions, loggerFactory);
            _client = await McpClient.CreateAsync(
                _transport,
                new McpClientOptions
                {
                    InitializationTimeout = TimeSpan.FromSeconds(Math.Max(5, options.Value.StartupTimeoutSeconds))
                },
                loggerFactory,
                cancellationToken);

            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ResetClientAsync()
    {
        McpClient? clientToDispose;
        StdioClientTransport? transportToDispose;

        await _gate.WaitAsync();
        try
        {
            clientToDispose = _client;
            transportToDispose = _transport;
            _client = null;
            _transport = null;
        }
        finally
        {
            _gate.Release();
        }

        await DisposeResourceAsync(clientToDispose, "MCP client");
        await DisposeResourceAsync(transportToDispose, "MCP transport");
    }

    internal StdioClientTransportOptions ResolveTransportOptions()
    {
        var currentOptions = options.Value;
        if (string.IsNullOrWhiteSpace(currentOptions.Command))
        {
            throw new InvalidOperationException(
                "Agents:Mcp:Recordsets:Command must be configured. " +
                "For local development, set it in appsettings.Development.json. " +
                "For deployed environments, point it at the published Recordsets MCP server executable.");
        }

        var command = currentOptions.Command;
        var arguments = currentOptions.Arguments;
        var workingDirectory = string.IsNullOrWhiteSpace(currentOptions.WorkingDirectory)
            ? hostEnvironment.ContentRootPath
            : currentOptions.WorkingDirectory;
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings__DefaultConnection"] = configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
            ["DOTNET_ENVIRONMENT"] = hostEnvironment.EnvironmentName,
            ["ASPNETCORE_ENVIRONMENT"] = hostEnvironment.EnvironmentName
        };

        return new StdioClientTransportOptions
        {
            Name = "recordsets",
            Command = command,
            Arguments = arguments.ToList(),
            WorkingDirectory = workingDirectory,
            ShutdownTimeout = TimeSpan.FromSeconds(Math.Max(5, currentOptions.StartupTimeoutSeconds)),
            EnvironmentVariables = environmentVariables,
            StandardErrorLines = line => logger.LogDebug("Recordsets MCP stderr: {Line}", line)
        };
    }

    private async Task DisposeResourceAsync(object? resource, string resourceName)
    {
        try
        {
            switch (resource)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to dispose {ResourceName}", resourceName);
        }
    }

    private static JsonObject CreateMeta(AgentMcpCallContext callContext)
    {
        var meta = new JsonObject
        {
            ["source"] = "records.agents"
        };

        if (!string.IsNullOrWhiteSpace(callContext.TenantId))
        {
            meta["tenantId"] = callContext.TenantId;
        }

        if (!string.IsNullOrWhiteSpace(callContext.ActorId))
        {
            meta["actorId"] = callContext.ActorId;
        }

        if (!string.IsNullOrWhiteSpace(callContext.CorrelationId))
        {
            meta["correlationId"] = callContext.CorrelationId;
        }

        return meta;
    }

    private static IReadOnlyDictionary<string, object?> ToArgumentsDictionary(JsonObject arguments)
    {
        return arguments.ToDictionary(
            pair => pair.Key,
            pair => pair.Value is null
                ? null
                : JsonSerializer.Deserialize<object>(pair.Value.ToJsonString(), AgentJsonSerializer.Options));
    }

    internal static string SerializeToolResult(CallToolResult result)
    {
        if (TrySerializeStructuredValue(result.StructuredContent, out var structuredJson))
        {
            return structuredJson;
        }

        var toolResultBlocks = result.Content.OfType<ToolResultContentBlock>().ToArray();
        if (toolResultBlocks.Length > 0)
        {
            foreach (var toolResultBlock in toolResultBlocks)
            {
                if (TrySerializeStructuredValue(toolResultBlock.StructuredContent, out structuredJson))
                {
                    return structuredJson;
                }
            }

            var nestedContent = toolResultBlocks
                .SelectMany(toolResultBlock => toolResultBlock.Content)
                .ToArray();
            if (nestedContent.Length > 0)
            {
                return JsonSerializer.Serialize(nestedContent, AgentJsonSerializer.Options);
            }
        }

        if (result.Content.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(result.Content, AgentJsonSerializer.Options);
    }

    private static bool TrySerializeStructuredValue(object? structuredContent, out string json)
    {
        if (structuredContent is JsonElement structuredElement)
        {
            json = structuredElement.GetRawText();
            return true;
        }

        if (structuredContent is JsonDocument structuredDocument)
        {
            json = structuredDocument.RootElement.GetRawText();
            return true;
        }

        if (structuredContent is JsonNode structuredNode)
        {
            json = structuredNode.ToJsonString(AgentJsonSerializer.Options);
            return true;
        }

        if (structuredContent is not null)
        {
            json = JsonSerializer.Serialize(structuredContent, AgentJsonSerializer.Options);
            return true;
        }

        json = string.Empty;
        return false;
    }
}
