using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Clocks.Application.Commands.Clocks.Complete;
using Records.Clocks.Application.Commands.Clocks.Pause;
using Records.Clocks.Application.Commands.Clocks.Resume;
using Records.Clocks.Application.Commands.Clocks.Start;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Presentation.Controllers;

[ApiController]
[Route("api/recordsets/{recordsetId}/records/{recordId:int}/clocks")]
public sealed class ClocksController(
    IMediator mediator,
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

        var id = await mediator.Send(command, cancellationToken);
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
        if (!UlidId.TryParse(recordsetId, out _))
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

        await mediator.Send(command, cancellationToken);
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
        if (!UlidId.TryParse(recordsetId, out _))
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

        await mediator.Send(command, cancellationToken);
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
        if (!UlidId.TryParse(recordsetId, out _))
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

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
