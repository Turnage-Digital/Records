using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands.CreateRecord;
using Records.Recordsets.Application.Commands.CreateRecordset;
using Records.Recordsets.Application.Commands.UpdateRecord;
using Records.Recordsets.Application.Commands.UpdateRecordsetSchema;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Presentation.Controllers;

[ApiController]
[Route("api/recordsets")]
public sealed class RecordsetsController(
    IMediator mediator,
    IRecordsetQueries recordsetQueries,
    IRecordQueries recordQueries
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecordsetSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var recordsets = await recordsetQueries.ListAsync(cancellationToken);
        return Ok(recordsets);
    }

    [HttpGet("{recordsetId}")]
    public async Task<ActionResult<RecordsetSummaryDto>> Get(string recordsetId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var recordset = await recordsetQueries.GetByIdAsync(recordsetUlid, cancellationToken);
        if (recordset is null)
        {
            return NotFound();
        }

        return Ok(recordset);
    }

    [HttpPost]
    public async Task<ActionResult<UlidId>> Create(CreateRecordsetCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPost("{recordsetId}/schema")]
    public async Task<IActionResult> UpdateSchema(
        string recordsetId,
        UpdateRecordsetSchemaCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (recordsetUlid != command.RecordsetId)
        {
            return BadRequest("Route recordsetId does not match payload.");
        }

        await mediator.Send(command, cancellationToken);
        return Ok();
    }

    [HttpGet("{recordsetId}/records")]
    public async Task<ActionResult<IReadOnlyList<RecordDto>>> ListRecords(
        string recordsetId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var records = await recordQueries.ListAsync(recordsetUlid, cancellationToken);
        return Ok(records);
    }

    [HttpGet("{recordsetId}/records/{recordId:int}")]
    public async Task<ActionResult<RecordDto>> GetRecord(
        string recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var record = await recordQueries.GetByIdAsync(recordsetUlid, recordId, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    [HttpPost("{recordsetId}/records")]
    public async Task<IActionResult> CreateRecord(
        string recordsetId,
        CreateRecordCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (recordsetUlid != command.RecordsetId)
        {
            return BadRequest("Route recordsetId does not match payload.");
        }

        await mediator.Send(command, cancellationToken);
        return Ok();
    }

    [HttpPost("{recordsetId}/records/{recordId:int}")]
    public async Task<IActionResult> UpdateRecord(
        string recordsetId,
        int recordId,
        UpdateRecordCommand command,
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

        await mediator.Send(command, cancellationToken);
        return Ok();
    }
}