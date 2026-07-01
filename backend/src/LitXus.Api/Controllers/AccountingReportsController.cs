using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Queries.GetBalanceSheet;
using LitXus.Application.Modules.Accounting.Queries.GetGeneralLedger;
using LitXus.Application.Modules.Accounting.Queries.GetIncomeStatement;
using LitXus.Application.Modules.Accounting.Queries.GetTrialBalance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounting/reports")]
[RequireModule(Module.Accounting)]
[RequirePermission("Accounting.Reports.Read")]
public class AccountingReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly? asOfDate)
    {
        var result = await mediator.Send(new GetTrialBalanceQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateOnly? asOfDate)
    {
        var result = await mediator.Send(new GetBalanceSheetQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetIncomeStatementQuery(
            from ?? new DateOnly(today.Year, 1, 1),
            to ?? today));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("general-ledger")]
    public async Task<IActionResult> GeneralLedger([FromQuery] Guid accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetGeneralLedgerQuery(
            accountId,
            from ?? new DateOnly(today.Year, 1, 1),
            to ?? today));
        return Ok(new { data = result, meta = (object?)null });
    }
}
