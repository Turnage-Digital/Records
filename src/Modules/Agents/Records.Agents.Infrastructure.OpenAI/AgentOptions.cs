namespace Records.Agents.Infrastructure.OpenAI;

public sealed class AgentLlmOptions
{
    public string Provider { get; init; } = "OpenAI";
    public string Model { get; init; } = "gpt-5-mini";
    public string? ApiKey { get; init; }
    public string? BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxToolCallsPerTurn { get; init; } = 8;
}

public sealed class RecordsetsMcpOptions
{
    public string? Command { get; init; }
    public string[] Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public int StartupTimeoutSeconds { get; init; } = 30;
    public int CallTimeoutSeconds { get; init; } = 30;
}
