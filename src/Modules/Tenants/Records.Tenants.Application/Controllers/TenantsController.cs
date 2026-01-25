using MediatR;
using Microsoft.AspNetCore.Mvc;
using Records.Tenants.Application.Commands.CreateTenant;
using Records.Tenants.Application.Commands.DisableTenant;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;

namespace Records.Tenants.Application.Controllers;

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

    [HttpGet("{tenantId:guid}")]
    public async Task<ActionResult<TenantSummaryDto>> Get(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await tenantQueries.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Ok(id);
    }

    [HttpPost("{tenantId:guid}/disable")]
    public async Task<IActionResult> Disable(Guid tenantId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DisableTenantCommand(tenantId), cancellationToken);
        return Ok();
    }
}