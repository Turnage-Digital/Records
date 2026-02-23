using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Records.Core.Contracts.Security;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Contracts.Queries;

namespace Records.Tenants.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireGlobalAdmin)]
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
    public async Task<ActionResult<TenantSummaryDto>> Create(
        CreateTenantCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = await mediator.Send(command, cancellationToken);
        var tenant = await tenantQueries.GetByIdAsync(id, cancellationToken);
        return tenant is null
            ? Created($"/api/tenants/{id}", new { tenantId = id })
            : Created($"/api/tenants/{id}", tenant);
    }

    [HttpPost("{tenantId}/disable")]
    public async Task<IActionResult> Disable(string tenantId, CancellationToken cancellationToken)
    {
        if (!UlidId.TryParse(tenantId, out var tenantUlid))
        {
            return BadRequest("Invalid tenant id format.");
        }

        await mediator.Send(new DisableTenantCommand(tenantUlid), cancellationToken);
        return NoContent();
    }
}