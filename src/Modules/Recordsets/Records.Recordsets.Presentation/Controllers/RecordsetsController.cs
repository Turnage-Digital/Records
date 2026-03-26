using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Contracts.Dtos;
using Records.Recordsets.Contracts.Queries;

namespace Records.Recordsets.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/recordsets")]
public sealed class RecordsetsController(
    IMediator mediator,
    IRecordsetQueries recordsetQueries,
    IRecordQueries recordQueries
) : ControllerBase
{
    [HttpGet("names")]
    public async Task<ActionResult<IReadOnlyList<RecordsetNameDto>>> ListNames(CancellationToken cancellationToken)
    {
        var names = await recordsetQueries.ListNamesAsync(cancellationToken);
        return Ok(names);
    }

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
    public async Task<ActionResult<RecordsetSummaryDto>> Create(
        CreateRecordsetCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = await mediator.Send(command, cancellationToken);
        var recordset = await recordsetQueries.GetByIdAsync(id, cancellationToken);

        return recordset is null
            ? Created($"/api/recordsets/{id}", new { recordsetId = id })
            : Created($"/api/recordsets/{id}", recordset);
    }

    [HttpGet("{recordsetId}/itemDefinition")]
    public async Task<ActionResult<RecordsetItemDefinitionDto>> GetItemDefinition(
        string recordsetId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var definition = await recordsetQueries.GetItemDefinitionAsync(recordsetUlid, cancellationToken);
        if (definition is null)
        {
            return NotFound();
        }

        return Ok(definition);
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
        return NoContent();
    }

    [HttpGet("{recordsetId}/records")]
    public async Task<ActionResult<RecordsetPagedRecordsDto>> ListRecords(
        string recordsetId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? field = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (page < 0 || pageSize <= 0)
        {
            return BadRequest("Invalid pagination arguments.");
        }

        var effectivePageSize = Math.Min(pageSize, 200);
        var pageResult = await recordQueries.GetPageAsync(
            recordsetUlid,
            page,
            effectivePageSize,
            status,
            field,
            sort,
            cancellationToken);
        if (pageResult is null)
        {
            return NotFound();
        }

        return Ok(pageResult);
    }

    [HttpGet("{recordsetId}/records/{recordId:int}")]
    public async Task<ActionResult<RecordItemDetailsDto>> GetRecord(
        string recordsetId,
        int recordId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var record = await recordQueries.GetDetailsAsync(recordsetUlid, recordId, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    [HttpGet("{recordsetId}/history")]
    public async Task<ActionResult<HistoryPageDto>> GetRecordsetHistory(
        string recordsetId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (page < 0 || pageSize <= 0)
        {
            return BadRequest("Invalid pagination arguments.");
        }

        var recordset = await recordsetQueries.GetByIdAsync(recordsetUlid, cancellationToken);
        if (recordset is null)
        {
            return NotFound();
        }

        var history = await recordQueries.GetRecordsetHistoryAsync(
            recordsetUlid,
            page,
            pageSize,
            cancellationToken);
        return Ok(history);
    }

    [HttpGet("{recordsetId}/records/{recordId:int}/history")]
    public async Task<ActionResult<HistoryPageDto>> GetRecordHistory(
        string recordsetId,
        int recordId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        if (page < 0 || pageSize <= 0)
        {
            return BadRequest("Invalid pagination arguments.");
        }

        var history = await recordQueries.GetRecordHistoryAsync(
            recordsetUlid,
            recordId,
            page,
            pageSize,
            cancellationToken);
        if (history is null)
        {
            return NotFound();
        }

        return Ok(history);
    }

    [HttpPost("{recordsetId}/records")]
    public async Task<ActionResult<CreateRecordResult>> CreateRecord(
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

        var result = await mediator.Send(command, cancellationToken);
        return Created($"/api/recordsets/{recordsetId}/records/{result.RecordId}", result);
    }

    [HttpPut("{recordsetId}/records/{recordId:int}")]
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
        return NoContent();
    }
}