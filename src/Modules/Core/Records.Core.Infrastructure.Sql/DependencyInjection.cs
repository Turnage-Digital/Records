using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Core.Contracts.Events;

namespace Records.Core.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<CoreDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(CoreDbContext).Assembly.FullName)));

        // Event store infrastructure
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddSingleton<IDomainEventSerializer, DomainEventSerializer>();

        return services;
    }
}