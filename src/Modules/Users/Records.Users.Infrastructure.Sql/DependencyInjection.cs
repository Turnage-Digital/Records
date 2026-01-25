using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Records.Users.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructureSql(
        this IServiceCollection services,
        string connectionString
    )
    {
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        services.AddDbContext<UsersDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName)));

        return services;
    }
}