using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.MatchBankStatementLine;
using LitXus.Application.Modules.Accounting.Commands.UnmatchBankStatementLine;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record MatchBankStatementLineRequest(Guid GLEntryLineId);

[ApiController]
[Authorize]
[Route("api/v1/accounting/bank-statement-lines")]
[RequireModule(Module.Accounting)]
public class BankStatementLinesController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/match")]
    [RequirePermission("Accounting.BankAccount.Update")]
    public async Task<IActionResult> Match(Guid id, [FromBody] MatchBankStatementLineRequest request)
    {
        var result = await mediator.Send(new MatchBankStatementLineCommand(id, request.GLEntryLineId));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/unmatch")]
    [RequirePermission("Accounting.BankAccount.Update")]
    public async Task<IActionResult> Unmatch(Guid id)
    {
        var result = await mediator.Send(new UnmatchBankStatementLineCommand(id));
        return Ok(new { data = result, meta = (object?)null });
    }
}
