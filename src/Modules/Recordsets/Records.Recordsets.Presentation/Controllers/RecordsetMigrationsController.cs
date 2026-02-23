using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Application.Queries;

namespace Records.Recordsets.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/recordsets/{recordsetId}/migrations")]
public sealed class RecordsetMigrationsController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MigrationResult>> RunMigration(
        string recordsetId,
        RunMigrationRequest? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return BadRequest(new { message = "A migration request body is required." });
        }

        if (!UlidId.TryParse(recordsetId, out var recordsetUlid))
        {
            return BadRequest("Invalid recordset id format.");
        }

        var command = new RunMigrationCommand(
            recordsetUlid,
            request.Plan,
            request.Mode,
            currentUserAccess.GetCurrentUserIdOrThrow(),
            DateTimeOffset.UtcNow);

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{correlationId}")]
    public async Task<ActionResult<RecordsetMigrationProgress>> GetStatus(
        string recordsetId,
        string correlationId,
        CancellationToken cancellationToken
    )
    {
        if (!UlidId.TryParse(recordsetId, out var recordsetUlid) ||
            !UlidId.TryParse(correlationId, out var correlationUlid))
        {
            return BadRequest("Invalid recordset or correlation id format.");
        }

        var result = await mediator.Send(
            new GetRecordsetMigrationJobStatusQuery(
                recordsetUlid,
                correlationUlid),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}