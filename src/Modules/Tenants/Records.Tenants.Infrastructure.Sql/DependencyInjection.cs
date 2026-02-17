using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Tenants.Contracts;
using Records.Tenants.Contracts.Queries;
using Records.Tenants.Domain.Interfaces;
using Records.Tenants.Infrastructure.Sql.Projections;
using Records.Tenants.Infrastructure.Sql.Queries;

namespace Records.Tenants.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantsInfrastructureSql(
        this IServiceCollection services,
        string connectionString
    )
    {
        ServerVersion serverVersion;
        try
        {
            serverVersion = ServerVersion.AutoDetect(connectionString);
        }
        catch
        {
            serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
        }

        services.AddDbContext<TenantsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(TenantsDbContext).Assembly.FullName)));

        services.AddScoped<ITenantsUnitOfWork, TenantsUnitOfWork>();
        services.AddScoped<ITenantProjectionWriter, TenantProjectionWriter>();
        services.AddScoped<ITenantQueries, TenantQueries>();

        return services;
    }
}
