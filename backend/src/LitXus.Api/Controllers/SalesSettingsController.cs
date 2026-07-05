using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Commands.ConfigureSalesSettings;
using LitXus.Application.Modules.Sales.Queries.GetSalesSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

/// <summary>Not in the original 15-endpoint API spec — added since SalesSettings (the GL account
/// mapping Sales auto-posting needs) has to be viewable/configurable from somewhere.</summary>
[ApiController]
[Authorize]
[Route("api/v1/sales/settings")]
[RequireModule(Module.Sales)]
public class SalesSettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Sales.Settings.Update")]
    public async Task<IActionResult> Get()
    {
        var result = await mediator.Send(new GetSalesSettingsQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPut]
    [RequirePermission("Sales.Settings.Update")]
    public async Task<IActionResult> Configure([FromBody] ConfigureSalesSettingsCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { data = result, meta = (object?)null });
    }
}
