using LitXus.Api.Filters;
using LitXus.Application.Modules.Licensing.Commands.ApplyLicenseKey;
using LitXus.Application.Modules.Licensing.Queries.GetLicense;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/license")]
public class AdminLicenseController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Admin.License.Read")]
    public async Task<IActionResult> Get()
    {
        var result = await mediator.Send(new GetLicenseQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("apply-key")]
    [RequirePermission("Admin.License.Update")]
    public async Task<IActionResult> ApplyKey([FromBody] ApplyLicenseKeyCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { data = result, meta = (object?)null });
    }
}
