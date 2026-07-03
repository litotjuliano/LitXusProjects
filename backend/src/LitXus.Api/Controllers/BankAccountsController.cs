using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.CreateBankAccount;
using LitXus.Application.Modules.Accounting.Commands.ImportBankStatementLines;
using LitXus.Application.Modules.Accounting.Queries.GetBankAccounts;
using LitXus.Application.Modules.Accounting.Queries.GetBankStatementLines;
using LitXus.Application.Modules.Accounting.Queries.GetReconciliationStatus;
using LitXus.Application.Modules.Accounting.Queries.GetUnmatchedGLEntryLines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounting/bank-accounts")]
[RequireModule(Module.Accounting)]
public class BankAccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Accounting.BankAccount.Read")]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetBankAccountsQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Accounting.BankAccount.Create")]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { data = result, meta = (object?)null });
    }

    [HttpGet("{id:guid}/statement-lines")]
    [RequirePermission("Accounting.BankAccount.Read")]
    public async Task<IActionResult> GetStatementLines(Guid id)
    {
        var result = await mediator.Send(new GetBankStatementLinesQuery(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/statement-lines/import")]
    [RequirePermission("Accounting.BankAccount.Update")]
    public async Task<IActionResult> ImportStatementLines(Guid id, IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync();

        var importedCount = await mediator.Send(new ImportBankStatementLinesCommand(id, csvContent));
        return Ok(new { data = new { importedCount }, meta = (object?)null });
    }

    [HttpGet("{id:guid}/unmatched-gl-lines")]
    [RequirePermission("Accounting.BankAccount.Read")]
    public async Task<IActionResult> GetUnmatchedGLLines(Guid id)
    {
        var result = await mediator.Send(new GetUnmatchedGLEntryLinesQuery(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("{id:guid}/reconciliation-status")]
    [RequirePermission("Accounting.BankAccount.Read")]
    public async Task<IActionResult> GetReconciliationStatus(Guid id)
    {
        var result = await mediator.Send(new GetReconciliationStatusQuery(id));
        return Ok(new { data = result, meta = (object?)null });
    }
}
