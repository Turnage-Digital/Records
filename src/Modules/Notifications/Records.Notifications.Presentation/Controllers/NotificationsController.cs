using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Application;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/notifications")]
public sealed class NotificationsController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess,
    INotificationQueries queries
) : ControllerBase
{
    [HttpGet("{notificationId}")]
    public async Task<ActionResult<NotificationDetailsDto>> Get(
        string notificationId,
        CancellationToken cancellationToken
    )
    {
        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var notification = await queries.GetByIdAsync(notificationId, currentUserId.ToString(), cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        return Ok(notification);
    }

    [HttpGet]
    public async Task<ActionResult<NotificationListPageDto>> List(
        [FromQuery] string? recordsetId,
        [FromQuery] DateTimeOffset? since,
        [FromQuery] bool? unread,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default
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

        var effectivePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);
        var effectivePage = Math.Max(0, page);

        var result = await queries.GetPageAsync(
            currentUserId.ToString(),
            recordsetId,
            since,
            unread,
            effectivePageSize,
            effectivePage,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("unreadCount")]
    public async Task<ActionResult<int>> GetUnreadCount(
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

        var count = await queries.GetUnreadCountAsync(currentUserId.ToString(), recordsetId, cancellationToken);
        return Ok(count);
    }

    [HttpPost]
    public async Task<ActionResult<CreateNotificationResult>> Create(
        CreateNotificationCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Created($"/api/notifications/{result.Value.NotificationId}", result.Value);
    }

    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkRead(
        string notificationId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(notificationId, out var notificationUlid))
        {
            return BadRequest("Invalid notification id format.");
        }

        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var command = new MarkNotificationReadCommand(
            notificationUlid,
            currentUserId.ToString(),
            DateTimeOffset.UtcNow);
        command.UserId = currentUserId.ToString();

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error == ResultErrors.NotFound
                ? NotFound()
                : BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPost("readAll")]
    public async Task<IActionResult> MarkAllRead(
        [FromBody] MarkAllReadBody? body,
        CancellationToken cancellationToken
    )
    {
        if (!currentUserAccess.TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        UlidId? scopedRecordsetId = null;
        if (!string.IsNullOrWhiteSpace(body?.RecordsetId))
        {
            if (!UlidId.TryParse(body.RecordsetId, out var parsedRecordsetId))
            {
                return BadRequest("Invalid recordset id format.");
            }

            scopedRecordsetId = parsedRecordsetId;
        }

        var command = new MarkAllNotificationsReadCommand(body?.Before, scopedRecordsetId);
        command.UserId = currentUserId.ToString();
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error == ResultErrors.Forbidden
                ? Unauthorized()
                : BadRequest(result.Error);
        }

        return Ok();
    }

    public sealed record MarkAllReadBody(DateTimeOffset? Before, string? RecordsetId);
}
