using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Records.Notifications.Contracts.Projections;
using Records.Notifications.Contracts.Queries;
using Records.Notifications.Domain;
using Records.Notifications.Domain.Services;

namespace Records.Notifications.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));

        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<INotificationRuleQueries, NotificationRuleQueries>();
        services.AddScoped<INotificationProjectionWriter, NotificationProjectionWriter>();
        services.AddScoped<INotificationTriggerEvaluator, NotificationTriggerEvaluator>();

        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();
        services.AddScoped<INotificationProvider, LoggingInAppProvider>();
        services.AddScoped<INotificationProvider, LoggingPushProvider>();

        return services;
    }
}