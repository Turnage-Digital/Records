using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Clocks.Application.Commands.Clocks.Complete;
using Records.Clocks.Application.Commands.Clocks.Pause;
using Records.Clocks.Application.Commands.Clocks.Resume;
using Records.Clocks.Application.Commands.Clocks.Start;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/recordsets/{recordsetId}/records/{recordId:int}/clocks")]
public sealed class ClocksController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess,
    IClockQueries queries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClockDto>>> List(
        string recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var clocks = await queries.ListByRecordAsync(recordsetUlid, recordId, cancellationToken);
        foreach (var clock in clocks)
        {
            var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
                clock.TenantId,
                cancellationToken);
            if (!canOperateTenant)
            {
                return Forbid();
            }
        }

        return Ok(clocks);
    }

    [HttpGet("{clockId}")]
    public async Task<ActionResult<ClockDto>> Get(
        string recordsetId,
        int recordId,
        string clockId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (!UlidId.TryParse(clockId, out var clockUlid))
        {
            return BadRequest("Invalid clock id format.");
        }

        var clock = await queries.GetByIdAsync(clockUlid, cancellationToken);
        if (clock is null || clock.RecordsetId != recordsetUlid || clock.RecordId != recordId)
        {
            return NotFound();
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
            clock.TenantId,
            cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        return Ok(clock);
    }

    [HttpPost]
    public async Task<ActionResult<ClockDto>> Start(
        string recordsetId,
        int recordId,
        StartClockCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (recordsetUlid != command.RecordsetId || recordId != command.RecordId)
        {
            return BadRequest("Route identifiers do not match payload.");
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(command.TenantId, cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            StartedBy = actorId,
            StartedAt = DateTimeOffset.UtcNow
        };

        var id = await mediator.Send(effectiveCommand, cancellationToken);
        var clock = await queries.GetByIdAsync(id, cancellationToken);
        return clock is null
            ? Created($"/api/recordsets/{recordsetId}/records/{recordId}/clocks/{id}", new { clockId = id })
            : Created($"/api/recordsets/{recordsetId}/records/{recordId}/clocks/{id}", clock);
    }

    [HttpPost("{clockId}/pause")]
    public async Task<IActionResult> Pause(
        string recordsetId,
        int recordId,
        string clockId,
        PauseClockCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (!UlidId.TryParse(clockId, out var clockUlid))
        {
            return BadRequest("Invalid clock id format.");
        }

        if (clockUlid != command.ClockId)
        {
            return BadRequest("Route clockId does not match payload.");
        }

        var clock = await queries.GetByIdAsync(clockUlid, cancellationToken);
        if (clock is null || clock.RecordsetId != recordsetUlid || clock.RecordId != recordId)
        {
            return NotFound();
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
            clock.TenantId,
            cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            PausedBy = actorId,
            PausedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }

    [HttpPost("{clockId}/resume")]
    public async Task<IActionResult> Resume(
        string recordsetId,
        int recordId,
        string clockId,
        ResumeClockCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (!UlidId.TryParse(clockId, out var clockUlid))
        {
            return BadRequest("Invalid clock id format.");
        }

        if (clockUlid != command.ClockId)
        {
            return BadRequest("Route clockId does not match payload.");
        }

        var clock = await queries.GetByIdAsync(clockUlid, cancellationToken);
        if (clock is null || clock.RecordsetId != recordsetUlid || clock.RecordId != recordId)
        {
            return NotFound();
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
            clock.TenantId,
            cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            ResumedBy = actorId,
            ResumedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }

    [HttpPost("{clockId}/complete")]
    public async Task<IActionResult> Complete(
        string recordsetId,
        int recordId,
        string clockId,
        CompleteClockCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (!UlidId.TryParse(clockId, out var clockUlid))
        {
            return BadRequest("Invalid clock id format.");
        }

        if (clockUlid != command.ClockId)
        {
            return BadRequest("Route clockId does not match payload.");
        }

        var clock = await queries.GetByIdAsync(clockUlid, cancellationToken);
        if (clock is null || clock.RecordsetId != recordsetUlid || clock.RecordId != recordId)
        {
            return NotFound();
        }

        var canOperateTenant = await currentUserAccess.CanOperateTenantAsync(
            clock.TenantId,
            cancellationToken);
        if (!canOperateTenant)
        {
            return Forbid();
        }

        var actorId = currentUserAccess.GetCurrentUserIdOrThrow();
        var effectiveCommand = command with
        {
            CompletedBy = actorId,
            CompletedAt = DateTimeOffset.UtcNow
        };

        await mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }
}
