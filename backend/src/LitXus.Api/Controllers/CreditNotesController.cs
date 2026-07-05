using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Commands.CreateCreditNote;
using LitXus.Application.Modules.Sales.Queries.GetCreditNoteById;
using LitXus.Application.Modules.Sales.Queries.GetCreditNotes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sales/credit-notes")]
[RequireModule(Module.Sales)]
public class CreditNotesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Sales.CreditNote.Read")]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetCreditNotesQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Sales.CreditNote.Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetCreditNoteByIdQuery(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Sales.CreditNote.Create")]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { data = result, meta = (object?)null });
    }
}
