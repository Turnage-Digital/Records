using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands.CreateTenant;
using Records.Tenants.Application.Commands.DisableTenant;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;

namespace Records.Tenants.Presentation.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(IMediator mediator, ITenantQueries tenantQueries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var tenants = await tenantQueries.ListAsync(cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("{tenantId}")]
    public async Task<ActionResult<TenantSummaryDto>> Get(string tenantId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(tenantId, out var tenantUlid))
        {
            return BadRequest("Invalid tenant id format.");
        }

        var tenant = await tenantQueries.GetByIdAsync(tenantUlid, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<UlidId>> Create(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPost("{tenantId}/disable")]
    public async Task<IActionResult> Disable(string tenantId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(tenantId, out var tenantUlid))
        {
            return BadRequest("Invalid tenant id format.");
        }

        await mediator.Send(new DisableTenantCommand(tenantUlid), cancellationToken);
        return Ok();
    }
}