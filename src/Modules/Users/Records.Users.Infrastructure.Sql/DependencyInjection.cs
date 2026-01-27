using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Users.Contracts;
using Records.Users.Contracts.Projections;
using Records.Users.Contracts.Queries;
using Records.Users.Domain.Interfaces;
using Records.Users.Infrastructure.Sql.Projections;
using Records.Users.Infrastructure.Sql.Queries;

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

        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserAccessQueries, UserAccessQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IUserRoleMembershipQueries, UserRoleMembershipQueries>();
        services.AddScoped<IUserProjectionWriter, UserProjectionWriter>();

        return services;
    }
}