using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands.NotificationRules.Create;
using Records.Notifications.Application.Commands.NotificationRules.Delete;
using Records.Notifications.Application.Commands.NotificationRules.Update;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/notifications/rules")]
public sealed class NotificationRulesController(
    IMediator mediator,
    INotificationRuleQueries ruleQueries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationRuleDto>>> Get(
        [FromQuery] string? listId,
        [FromQuery] string? userId,
        CancellationToken cancellationToken
    )
    {
        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(listId) && !UlidId.TryParse(listId, out _))
        {
            return BadRequest("Invalid list id format.");
        }

        var rules = await ruleQueries.GetByListAsync(resolvedUserId, listId, cancellationToken);
        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<NotificationRuleDto>> Create(
        CreateNotificationRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(request.ListId, out var recordsetId))
        {
            return BadRequest("Invalid list id format.");
        }

        if (!UlidId.TryParse(request.TenantId, out var tenantId))
        {
            return BadRequest("Invalid tenant id format.");
        }

        var resolvedUserId = ResolveUserId(request.UserId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        var command = new CreateNotificationRuleCommand(
            tenantId,
            recordsetId,
            resolvedUserId,
            request.Trigger,
            request.Channels,
            request.Schedule,
            request.TemplateId,
            request.IsActive,
            DateTimeOffset.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);
        return Created($"/api/notifications/rules/{result.Id}", result);
    }

    [HttpPut("{ruleId}")]
    public async Task<ActionResult<NotificationRuleDto>> Update(
        string ruleId,
        UpdateNotificationRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(ruleId, out var ruleUlid))
        {
            return BadRequest("Invalid rule id format.");
        }

        var resolvedUserId = ResolveUserId(request.UserId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        var command = new UpdateNotificationRuleCommand(
            ruleUlid,
            request.Trigger,
            request.Channels,
            request.Schedule,
            request.TemplateId,
            request.IsActive,
            resolvedUserId,
            DateTimeOffset.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> Delete(
        string ruleId,
        [FromQuery] string? userId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(ruleId, out var ruleUlid))
        {
            return BadRequest("Invalid rule id format.");
        }

        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        var command = new DeleteNotificationRuleCommand(ruleUlid, resolvedUserId, DateTimeOffset.UtcNow);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    private string? ResolveUserId(string? explicitUserId)
    {
        if (!string.IsNullOrWhiteSpace(explicitUserId))
        {
            return explicitUserId;
        }

        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
    }
}