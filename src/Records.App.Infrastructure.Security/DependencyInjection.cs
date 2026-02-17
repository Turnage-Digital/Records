using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Records.Core.Application;
using Records.Core.Contracts.Security;

namespace Records.App.Infrastructure.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddRecordsAppSecurity(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserAccess, CurrentUserAccess>();
        services.AddScoped<ICurrentUserInitializer, CurrentUserInitializer>();
        services.AddScoped<IAuthorizationHandler, RequireGlobalAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RequireOpsAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.RequireGlobalAdmin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new RequireGlobalAdminRequirement()))
            .AddPolicy(
                AuthorizationPolicies.RequireOps,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new RequireOpsRequirement()));

        return services;
    }
}
