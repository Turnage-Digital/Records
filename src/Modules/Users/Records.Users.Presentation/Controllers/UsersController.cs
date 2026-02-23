using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;
using Records.Users.Application.Commands;
using Records.Users.Contracts.Dtos;
using Records.Users.Contracts.Queries;
using Records.Users.Domain;

namespace Records.Users.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/users")]
public sealed class UsersController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess,
    IUserQueries userQueries,
    IUserRoleMembershipQueries roleMembershipQueries
) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var users = await userQueries.ListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{userId}")]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
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
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
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
    public async Task<ActionResult<UserSummaryDto>> Invite(
        InviteUserCommand command,
        CancellationToken cancellationToken
    )
    {
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
        if (!isGlobalAdmin)
        {
            if (command.Roles.Count == 0)
            {
                return Forbid();
            }

            foreach (var roleAssignment in command.Roles)
            {
                if (roleAssignment.Role != UserRole.Operations || roleAssignment.TenantId is null)
                {
                    return Forbid();
                }

                var canManageTenant = await currentUserAccess.CanManageTenantAsync(
                    roleAssignment.TenantId.Value,
                    cancellationToken);
                if (!canManageTenant)
                {
                    return Forbid();
                }
            }
        }

        var effectiveCommand = command with
        {
            InvitedAt = DateTimeOffset.UtcNow
        };

        var id = await mediator.Send(effectiveCommand, cancellationToken);
        var user = await userQueries.GetByIdAsync(id, cancellationToken);
        return user is null
            ? Created($"/api/users/{id}", new { userId = id })
            : Created($"/api/users/{id}", user);
    }

    [HttpPost("{userId}/suspend")]
    [Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
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
        return NoContent();
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

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);

        if (!isGlobalAdmin)
        {
            if (command.Role != UserRole.Operations || !command.TenantId.HasValue)
            {
                return Forbid();
            }

            var canManageTenant = await currentUserAccess.CanManageTenantAsync(
                command.TenantId.Value,
                cancellationToken);
            if (!canManageTenant)
            {
                return Forbid();
            }
        }

        var effectiveCommand = command with
        {
            GrantedBy = actorId,
            GrantedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
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

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);

        if (!isGlobalAdmin)
        {
            if (command.Role != UserRole.Operations || !command.TenantId.HasValue)
            {
                return Forbid();
            }

            var canManageTenant = await currentUserAccess.CanManageTenantAsync(
                command.TenantId.Value,
                cancellationToken);
            if (!canManageTenant)
            {
                return Forbid();
            }
        }

        var effectiveCommand = command with
        {
            RevokedBy = actorId
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }
}