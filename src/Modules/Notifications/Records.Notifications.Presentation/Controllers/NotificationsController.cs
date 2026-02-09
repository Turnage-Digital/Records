using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
        [FromQuery] string? listId,
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

        if (!string.IsNullOrWhiteSpace(listId) && !UlidId.TryParse(listId, out _))
        {
            return BadRequest("Invalid list id format.");
        }

        var effectivePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);
        var effectivePage = Math.Max(0, page);

        var result = await queries.GetPageAsync(
            resolvedUserId,
            listId,
            since,
            unread,
            effectivePageSize,
            effectivePage,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("unreadCount")]
    public async Task<ActionResult<int>> GetUnreadCount(
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

        var count = await queries.GetUnreadCountAsync(resolvedUserId, listId, cancellationToken);
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
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(notificationId, out var notificationUlid))
        {
            return BadRequest("Invalid notification id format.");
        }

        if (notificationUlid != command.NotificationId)
        {
            return BadRequest("Route notificationId does not match payload.");
        }

        if (string.IsNullOrWhiteSpace(command.ReaderUserId))
        {
            var resolvedUserId = ResolveUserId(null);
            if (string.IsNullOrWhiteSpace(resolvedUserId))
            {
                return Unauthorized();
            }

            command = command with { ReaderUserId = resolvedUserId };
        }

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

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
