using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands.NotificationRules.Create;
using Records.Notifications.Application.Commands.NotificationRules.Delete;
using Records.Notifications.Application.Commands.NotificationRules.Update;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/notifications/rules")]
public sealed class NotificationRulesController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess,
    INotificationRuleQueries ruleQueries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationRuleDto>>> Get(
        [FromQuery] string? recordsetId,
        CancellationToken cancellationToken
    )
    {
        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(recordsetId) && !UlidId.TryParse(recordsetId, out _))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var rules = await ruleQueries.GetByRecordsetAsync(currentUserId.ToString(), recordsetId, cancellationToken);
        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<NotificationRuleDto>> Create(
        CreateNotificationRuleRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(request.RecordsetId, out var recordsetId))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (!UlidId.TryParse(request.TenantId, out var tenantId))
        {
            return BadRequest("Invalid tenant id format.");
        }

        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var canManageTenant = await currentUserAccess.CanManageTenantAsync(tenantId, cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var command = new CreateNotificationRuleCommand(
            tenantId,
            recordsetId,
            currentUserId.ToString(),
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

        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var existingRule = await ruleQueries.GetByIdAsync(ruleId, cancellationToken);
        if (existingRule is null)
        {
            return NotFound();
        }

        if (!UlidId.TryParse(existingRule.TenantId, out var tenantId))
        {
            return BadRequest("Stored tenant id format is invalid.");
        }

        var canManageTenant = await currentUserAccess.CanManageTenantAsync(tenantId, cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var command = new UpdateNotificationRuleCommand(
            ruleUlid,
            request.Trigger,
            request.Channels,
            request.Schedule,
            request.TemplateId,
            request.IsActive,
            currentUserId.ToString(),
            DateTimeOffset.UtcNow
        );

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> Delete(
        string ruleId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(ruleId, out var ruleUlid))
        {
            return BadRequest("Invalid rule id format.");
        }

        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var existingRule = await ruleQueries.GetByIdAsync(ruleId, cancellationToken);
        if (existingRule is null)
        {
            return NotFound();
        }

        if (!UlidId.TryParse(existingRule.TenantId, out var tenantId))
        {
            return BadRequest("Stored tenant id format is invalid.");
        }

        var canManageTenant = await currentUserAccess.CanManageTenantAsync(tenantId, cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var command = new DeleteNotificationRuleCommand(
            ruleUlid,
            currentUserId.ToString(),
            DateTimeOffset.UtcNow);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
