using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Application;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Application.Commands;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;

namespace Records.Notifications.Presentation.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(
    IMediator mediator,
    INotificationQueries queries
) : ControllerBase
{
    [HttpGet("{notificationId}")]
    public async Task<ActionResult<NotificationDetailsDto>> Get(
        string notificationId,
        [FromQuery] string? userId,
        CancellationToken cancellationToken
    )
    {
        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        var notification = await queries.GetByIdAsync(notificationId, resolvedUserId, cancellationToken);
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
        [FromQuery] string? userId = null,
        CancellationToken cancellationToken = default
    )
    {
        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
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
            resolvedUserId,
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
        [FromQuery] string? userId,
        CancellationToken cancellationToken
    )
    {
        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(resolvedUserId))
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(recordsetId) && !UlidId.TryParse(recordsetId, out _))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var count = await queries.GetUnreadCountAsync(resolvedUserId, recordsetId, cancellationToken);
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

        var readerUserId = ResolveUserId(null);
        if (string.IsNullOrWhiteSpace(readerUserId))
        {
            return Unauthorized();
        }

        var command = new MarkNotificationReadCommand(
            notificationUlid,
            readerUserId,
            DateTimeOffset.UtcNow);

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
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error == ResultErrors.Forbidden
                ? Unauthorized()
                : BadRequest(result.Error);
        }

        return Ok();
    }

    private string? ResolveUserId(string? explicitUserId)
    {
        if (!string.IsNullOrWhiteSpace(explicitUserId))
        {
            return explicitUserId;
        }

        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
    }

    public sealed record MarkAllReadBody(DateTimeOffset? Before, string? RecordsetId);
}
