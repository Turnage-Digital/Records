using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Records.Agents.Application.Commands;
using Records.Agents.Contracts;
using Records.Agents.Contracts.Dtos;
using Records.Agents.Contracts.Queries;
using Records.Core.Contracts;

namespace Records.Agents.Presentation.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireOps)]
[Route("api/agents")]
public sealed class AgentsController(
    IMediator mediator,
    IAgentThreadQueries threadQueries,
    IAgentThreadStream threadStream
) : ControllerBase
{
    [HttpGet("threads")]
    public async Task<ActionResult<IReadOnlyList<AgentThreadSummaryDto>>> ListThreads(CancellationToken cancellationToken)
    {
        var threads = await threadQueries.ListAsync(cancellationToken);
        return Ok(threads);
    }

    [HttpGet("threads/{threadId}")]
    public async Task<ActionResult<AgentThreadDto>> GetThread(string threadId, CancellationToken cancellationToken)
    {
        var thread = await threadQueries.GetByIdAsync(threadId, cancellationToken);
        return thread is null ? NotFound() : Ok(thread);
    }

    [HttpPost("threads")]
    public async Task<ActionResult<AgentThreadSummaryDto>> CreateThread(
        CreateAgentThreadCommand command,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(command, cancellationToken);
        return Created($"/api/agents/threads/{result.Id}", result);
    }

    [HttpPost("threads/{threadId}/turns")]
    public async Task<ActionResult<AgentThreadDto>> PostTurn(
        string threadId,
        PostAgentTurnBody body,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new PostAgentTurnCommand(threadId, body.Message, body.PastedText),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("threads/{threadId}/proposals/{proposalId}/confirm")]
    public async Task<ActionResult<AgentThreadDto>> ConfirmProposal(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new ConfirmAgentProposalCommand(threadId, proposalId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("threads/{threadId}/proposals/{proposalId}/reject")]
    public async Task<ActionResult<AgentThreadDto>> RejectProposal(
        string threadId,
        string proposalId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new RejectAgentProposalCommand(threadId, proposalId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("threads/{threadId}/stream")]
    public async Task StreamThread(string threadId, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var streamEvent in threadStream.SubscribeAsync(threadId, cancellationToken))
        {
            var json = JsonSerializer.Serialize(streamEvent);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
    public sealed record PostAgentTurnBody(string Message, string? PastedText = null);
}
