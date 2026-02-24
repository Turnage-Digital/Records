using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Clocks.Application.Commands;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/clock-definitions")]
public sealed class ClockDefinitionsController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess,
    IClockDefinitionQueries queries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClockDefinitionDto>>> List(
        [FromQuery] string tenantId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(tenantId, out var tenantUlid))
        {
            return BadRequest("Invalid tenant id format.");
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(tenantUlid, cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        var definitions = await queries.ListByTenantAsync(tenantUlid, cancellationToken);
        return Ok(definitions);
    }

    [HttpGet("{definitionId}")]
    public async Task<ActionResult<ClockDefinitionDto>> Get(
        string definitionId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(definitionId, out var definitionUlid))
        {
            return BadRequest("Invalid definition id format.");
        }

        var definition = await queries.GetByIdAsync(definitionUlid, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
            definition.TenantId,
            cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        return Ok(definition);
    }

    [HttpPost]
    public async Task<ActionResult<ClockDefinitionDto>> Create(
        CreateClockDefinitionCommand command,
        CancellationToken cancellationToken
    )
    {
        var canManageTenant = await currentUserAccess.CanManageTenantAsync(command.TenantId, cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            CreatedBy = actorId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var id = await mediator.Send(effectiveCommand, cancellationToken);
        var definition = await queries.GetByIdAsync(id, cancellationToken);
        return definition is null
            ? Created($"/api/clock-definitions/{id}", new { definitionId = id })
            : Created($"/api/clock-definitions/{id}", definition);
    }

    [HttpPut("{definitionId}")]
    public async Task<IActionResult> Update(
        string definitionId,
        UpdateClockDefinitionCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(definitionId, out var definitionUlid))
        {
            return BadRequest("Invalid definition id format.");
        }

        if (definitionUlid != command.DefinitionId)
        {
            return BadRequest("Route definitionId does not match payload.");
        }

        var definition = await queries.GetByIdAsync(definitionUlid, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        var canManageTenant = await currentUserAccess.CanManageTenantAsync(
            definition.TenantId,
            cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            UpdatedBy = actorId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }

    [HttpPost("{definitionId}/disable")]
    public async Task<IActionResult> Disable(
        string definitionId,
        DisableClockDefinitionCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(definitionId, out var definitionUlid))
        {
            return BadRequest("Invalid definition id format.");
        }

        if (definitionUlid != command.DefinitionId)
        {
            return BadRequest("Route definitionId does not match payload.");
        }

        var definition = await queries.GetByIdAsync(definitionUlid, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        var canManageTenant = await currentUserAccess.CanManageTenantAsync(
            definition.TenantId,
            cancellationToken);
        if (!canManageTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            UpdatedBy = actorId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }
}