using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Records.App.ChangeFeed.EventHandlers;

namespace Records.App.ChangeFeed;

public static class DependencyInjection
{
    public static IServiceCollection AddChangeFeed(this IServiceCollection services)
    {
        services.AddSingleton<ChangeFeed>();
        services.AddTransient(typeof(INotificationHandler<>), typeof(ChangeFeedNotificationHandler<>));

        return services;
    }
}