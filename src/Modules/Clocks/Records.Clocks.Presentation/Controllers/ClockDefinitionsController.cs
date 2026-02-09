using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Clocks.Application.Commands.ClockDefinitions.Create;
using Records.Clocks.Application.Commands.ClockDefinitions.Disable;
using Records.Clocks.Application.Commands.ClockDefinitions.Update;
using Records.Clocks.Contracts.Dtos;
using Records.Clocks.Contracts.Queries;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Presentation.Controllers;

[ApiController]
[Route("api/clock-definitions")]
public sealed class ClockDefinitionsController(
    IMediator mediator,
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

        return Ok(definition);
    }

    [HttpPost]
    public async Task<ActionResult<ClockDefinitionDto>> Create(
        CreateClockDefinitionCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = await mediator.Send(command, cancellationToken);
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

        await mediator.Send(command, cancellationToken);
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

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
