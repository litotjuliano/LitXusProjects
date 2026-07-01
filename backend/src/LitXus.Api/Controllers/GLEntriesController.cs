using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;
using LitXus.Application.Modules.Accounting.Commands.PostGLEntry;
using LitXus.Application.Modules.Accounting.Commands.VoidGLEntry;
using LitXus.Application.Modules.Accounting.Queries.GetGLEntries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record VoidGLEntryRequest(string Reason);

[ApiController]
[Authorize]
[Route("api/v1/accounting/gl-entries")]
[RequireModule(Module.Accounting)]
public class GLEntriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Accounting.GLEntry.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var result = await mediator.Send(new GetGLEntriesQuery(status));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Accounting.GLEntry.Create")]
    public async Task<IActionResult> Create([FromBody] CreateGLEntryCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/post")]
    [RequirePermission("Accounting.GLEntry.Approve")]
    public async Task<IActionResult> Post(Guid id)
    {
        var result = await mediator.Send(new PostGLEntryCommand(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/void")]
    [RequirePermission("Accounting.GLEntry.Approve")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidGLEntryRequest request)
    {
        var result = await mediator.Send(new VoidGLEntryCommand(id, request.Reason));
        return Ok(new { data = result, meta = (object?)null });
    }
}
