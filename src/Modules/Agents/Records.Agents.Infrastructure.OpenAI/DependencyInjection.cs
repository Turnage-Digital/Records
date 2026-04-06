using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Records.Agents.Contracts;

namespace Records.Agents.Infrastructure.OpenAI;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentsInfrastructureOpenAi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AgentLlmOptions>(settings => configuration.GetSection("Agents:Llm").Bind(settings));
        services.Configure<RecordsetsMcpOptions>(settings => configuration.GetSection("Agents:Mcp:Recordsets").Bind(settings));
        services.AddSingleton<IAgentBackendRegistry, AgentBackendRegistry>();
        services.AddSingleton<IAgentBackend, RecordsetsAgentBackend>();
        services.AddScoped<IAgentConversationService, AgentConversationService>();
        services.AddScoped<IAgentProvider, OpenAiAgentProvider>();
        services.AddScoped<IAgentProposalExecutor, AgentProposalExecutor>();
        services.AddSingleton<IAgentLlmClient, OpenAiResponsesAgentClient>();
        services.AddSingleton<IRecordsetsMcpClient, RecordsetsMcpClient>();
        services.AddSingleton<IAgentThreadStream, AgentThreadStream>();

        return services;
    }
}
