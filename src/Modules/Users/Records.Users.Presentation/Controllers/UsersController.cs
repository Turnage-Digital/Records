using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Domain.ValueObjects;
using Records.Users.Application.Commands.GrantUserRole;
using Records.Users.Application.Commands.InviteUser;
using Records.Users.Application.Commands.RevokeUserRole;
using Records.Users.Application.Commands.SuspendUser;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;

namespace Records.Users.Presentation.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IMediator mediator,
    IUserQueries userQueries,
    IUserRoleMembershipQueries roleMembershipQueries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var users = await userQueries.ListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserSummaryDto>> Get(string userId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(userId, out var userUlid))
        {
            return BadRequest("Invalid user id format.");
        }

        var user = await userQueries.GetByIdAsync(userUlid, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpGet("{userId}/roles")]
    public async Task<ActionResult<IReadOnlyList<UserRoleMembershipDto>>> Roles(
        string userId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(userId, out var userUlid))
        {
            return BadRequest("Invalid user id format.");
        }

        var roles = await roleMembershipQueries.ListForUserAsync(userUlid, cancellationToken);
        return Ok(roles);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<UlidId>> Invite(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPost("{userId}/suspend")]
    public async Task<IActionResult> Suspend(
        string userId,
        SuspendUserCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(userId, out var userUlid))
        {
            return BadRequest("Invalid user id format.");
        }

        if (userUlid != command.UserId)
        {
            return BadRequest("Route userId does not match payload.");
        }

        await mediator.Send(command, cancellationToken);
        return Ok();
    }

    [HttpPost("{userId}/roles/grant")]
    public async Task<IActionResult> GrantRole(
        string userId,
        GrantUserRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(userId, out var userUlid))
        {
            return BadRequest("Invalid user id format.");
        }

        if (userUlid != command.UserId)
        {
            return BadRequest("Route userId does not match payload.");
        }

        await mediator.Send(command, cancellationToken);
        return Ok();
    }

    [HttpPost("{userId}/roles/revoke")]
    public async Task<IActionResult> RevokeRole(
        string userId,
        RevokeUserRoleCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(userId, out var userUlid))
        {
            return BadRequest("Invalid user id format.");
        }

        if (userUlid != command.UserId)
        {
            return BadRequest("Route userId does not match payload.");
        }

        await mediator.Send(command, cancellationToken);
        return Ok();
    }
}