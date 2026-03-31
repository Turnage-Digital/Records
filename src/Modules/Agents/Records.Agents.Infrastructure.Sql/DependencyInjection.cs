using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Projections;
using Records.Agents.Contracts.Queries;
using Records.Agents.Domain;

namespace Records.Agents.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<AgentsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(AgentsDbContext).Assembly.FullName)));

        services.AddScoped<IAgentsUnitOfWork, AgentsUnitOfWork>();
        services.AddScoped<IAgentThreadProjectionWriter, AgentThreadProjectionWriter>();
        services.AddScoped<IAgentThreadQueries, AgentThreadQueries>();
        services.AddScoped<IAgentConversationService, AgentConversationService>();
        services.AddScoped<IAgentProvider, RuleBasedAgentProvider>();
        services.AddScoped<IWorkspaceBackendAdapter, RecordsBackendAdapter>();
        services.AddSingleton<IAgentThreadStream, AgentThreadStream>();

        return services;
    }
}
