using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Recordsets.Contracts.Jobs;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Contracts.Queries;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Infrastructure.Sql.Jobs;
using Records.Recordsets.Infrastructure.Sql.Projections;
using Records.Recordsets.Infrastructure.Sql.Queries;
using Records.Recordsets.Infrastructure.Sql.Repositories;

namespace Records.Recordsets.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddRecordsetsInfrastructureSql(
        this IServiceCollection services,
        string connectionString
    )
    {
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        services.AddDbContext<RecordsetsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(RecordsetsDbContext).Assembly.FullName)));

        services.AddScoped<IRecordsetsUnitOfWork, RecordsetRepository>();
        services.AddScoped<IRecordsetMigrationJobWriter, RecordsetMigrationJobWriter>();
        services.AddScoped<IRecordsetProjectionWriter, RecordsetProjectionWriter>();
        services.AddScoped<IRecordsetQueries, RecordsetQueries>();
        services.AddScoped<IRecordQueries, RecordQueries>();
        services.AddScoped<IRecordsetMigrationJobQueries, RecordsetMigrationJobQueries>();

        return services;
    }
}