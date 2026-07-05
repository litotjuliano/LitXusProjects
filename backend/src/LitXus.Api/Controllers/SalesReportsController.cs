using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Queries.GetArAging;
using LitXus.Application.Modules.Sales.Queries.GetSalesSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sales/reports")]
[RequireModule(Module.Sales)]
[RequirePermission("Sales.Reports.Read")]
public class SalesReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("sales-summary")]
    public async Task<IActionResult> SalesSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string groupBy = "customer")
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetSalesSummaryQuery(from ?? new DateOnly(today.Year, 1, 1), to ?? today, groupBy));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("aging")]
    public async Task<IActionResult> Aging([FromQuery] DateOnly? asOfDate)
    {
        var result = await mediator.Send(new GetArAgingQuery(asOfDate));
        return Ok(new { data = result, meta = (object?)null });
    }
}
