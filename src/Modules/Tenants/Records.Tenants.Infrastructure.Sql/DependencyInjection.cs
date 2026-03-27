using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Tenants.Contracts.Projections;
using Records.Tenants.Contracts.Queries;
using Records.Tenants.Domain;

namespace Records.Tenants.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<TenantsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(TenantsDbContext).Assembly.FullName)));

        services.AddScoped<ITenantsUnitOfWork, TenantsUnitOfWork>();
        services.AddScoped<ITenantProjectionWriter, TenantProjectionWriter>();
        services.AddScoped<ITenantQueries, TenantQueries>();

        return services;
    }
}