using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Records.Core.Contracts.Security;

namespace Records.App.Infrastructure.Security;

public sealed class RequireGlobalAdminRequirement : IAuthorizationRequirement;

public sealed class RequireOpsRequirement : IAuthorizationRequirement;

public sealed class RequireGlobalAdminAuthorizationHandler(ICurrentUserAccess currentUserAccess)
    : AuthorizationHandler<RequireGlobalAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireGlobalAdminRequirement requirement
    )
    {
        if (await currentUserAccess.IsGlobalAdminAsync(GetCancellationToken(context)))
        {
            context.Succeed(requirement);
        }
    }

    private static CancellationToken GetCancellationToken(AuthorizationHandlerContext context)
    {
        return context.Resource switch
        {
            HttpContext httpContext => httpContext.RequestAborted,
            AuthorizationFilterContext filterContext => filterContext.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
    }
}

public sealed class RequireOpsAuthorizationHandler(ICurrentUserAccess currentUserAccess)
    : AuthorizationHandler<RequireOpsRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireOpsRequirement requirement
    )
    {
        if (await currentUserAccess.CanAccessOpsAsync(GetCancellationToken(context)))
        {
            context.Succeed(requirement);
        }
    }

    private static CancellationToken GetCancellationToken(AuthorizationHandlerContext context)
    {
        return context.Resource switch
        {
            HttpContext httpContext => httpContext.RequestAborted,
            AuthorizationFilterContext filterContext => filterContext.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
    }
}