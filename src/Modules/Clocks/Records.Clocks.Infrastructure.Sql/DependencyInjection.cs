using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Clocks.Contracts;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;

namespace Records.Clocks.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddClocksInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<ClocksDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(ClocksDbContext).Assembly.FullName)));

        services.AddScoped<IClocksUnitOfWork, ClocksUnitOfWork>();
        services.AddScoped<IClockDefinitionQueries, ClockDefinitionQueries>();
        services.AddScoped<IClockQueries, ClockQueries>();
        services.AddScoped<IClockDefinitionProjectionWriter, ClockDefinitionProjectionWriter>();
        services.AddScoped<IClockProjectionWriter, ClockProjectionWriter>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();
        services.AddScoped<ClocksEventProjectionRunner>();

        return services;
    }
}