using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.App.Server.Services;
using Records.Core.Contracts.Security;

namespace Records.App.Server.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/changes/stream")]
public sealed class ChangeStreamController(ChangeFeed feed) : ControllerBase
{
    [HttpGet]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var json in feed.Subscribe(cancellationToken))
        {
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}