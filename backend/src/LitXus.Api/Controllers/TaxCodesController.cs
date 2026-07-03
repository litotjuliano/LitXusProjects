using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.CreateTaxCode;
using LitXus.Application.Modules.Accounting.Queries.CalculateSst;
using LitXus.Application.Modules.Accounting.Queries.GetTaxCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record CalculateSstRequest(decimal SubTotal, Guid TaxCodeId);

[ApiController]
[Authorize]
[Route("api/v1/accounting")]
[RequireModule(Module.Accounting)]
public class TaxCodesController(IMediator mediator) : ControllerBase
{
    [HttpGet("tax-codes")]
    [RequirePermission("Accounting.TaxCode.Read")]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetTaxCodesQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("tax-codes")]
    [RequirePermission("Accounting.TaxCode.Create")]
    public async Task<IActionResult> Create([FromBody] CreateTaxCodeCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { data = result, meta = (object?)null });
    }

    [HttpPost("tax/calculate-sst")]
    [RequirePermission("Accounting.TaxCode.Read")]
    public async Task<IActionResult> CalculateSst([FromBody] CalculateSstRequest request)
    {
        var result = await mediator.Send(new CalculateSstQuery(request.SubTotal, request.TaxCodeId));
        return Ok(new { data = result, meta = (object?)null });
    }
}
