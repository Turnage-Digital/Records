using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Recordsets.Contracts;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddRecordsetsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<RecordsetsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(RecordsetsDbContext).Assembly.FullName)));

        services.AddScoped<IRecordsetsUnitOfWork, RecordsetsUnitOfWork>();
        services.AddScoped<IRecordsetMigrationJobWriter, RecordsetMigrationJobWriter>();
        services.AddScoped<IRecordsetProjectionWriter, RecordsetProjectionWriter>();
        services.AddScoped<IRecordsetQueries, RecordsetQueries>();
        services.AddScoped<IRecordQueries, RecordQueries>();
        services.AddScoped<IRecordsetMigrationJobQueries, RecordsetMigrationJobQueries>();

        return services;
    }
}