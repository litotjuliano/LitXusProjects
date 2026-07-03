using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.CreateAccount;
using LitXus.Application.Modules.Accounting.Commands.DeactivateAccount;
using LitXus.Application.Modules.Accounting.Commands.ReactivateAccount;
using LitXus.Application.Modules.Accounting.Commands.UpdateAccount;
using LitXus.Application.Modules.Accounting.Queries.GetAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record UpdateAccountRequest(string Name, Guid? ParentAccountId);

[ApiController]
[Authorize]
[Route("api/v1/accounting/accounts")]
[RequireModule(Module.Accounting)]
public class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Accounting.Account.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] bool includeInactive = false)
    {
        var result = await mediator.Send(new GetAccountsQuery(type, includeInactive));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Accounting.Account.Create")]
    public async Task<IActionResult> Create([FromBody] CreateAccountCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { data = result, meta = (object?)null });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Accounting.Account.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var result = await mediator.Send(new UpdateAccountCommand(id, request.Name, request.ParentAccountId));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission("Accounting.Account.Update")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await mediator.Send(new DeactivateAccountCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [RequirePermission("Accounting.Account.Update")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await mediator.Send(new ReactivateAccountCommand(id));
        return NoContent();
    }
}
