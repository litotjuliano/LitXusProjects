using LitXus.Api.Filters;
using LitXus.Application.Modules.Company.Commands.AddSignatory;
using LitXus.Application.Modules.Company.Commands.RemoveSignatory;
using LitXus.Application.Modules.Company.Commands.UpsertCompanyProfile;
using LitXus.Application.Modules.Company.Queries.GetCompanyProfile;
using LitXus.Application.Modules.Company.Queries.GetSignatories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/company")]
public class CompanyController(IMediator mediator) : ControllerBase
{
    [HttpGet("profile")]
    [RequirePermission("Company.Profile.Read")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await mediator.Send(new GetCompanyProfileQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPut("profile")]
    [RequirePermission("Company.Profile.Update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpsertCompanyProfileCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("signatories")]
    [RequirePermission("Company.Profile.Read")]
    public async Task<IActionResult> GetSignatories()
    {
        var result = await mediator.Send(new GetSignatoriesQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("signatories")]
    [RequirePermission("Company.Profile.Update")]
    public async Task<IActionResult> AddSignatory([FromBody] AddSignatoryCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpDelete("signatories/{id:guid}")]
    [RequirePermission("Company.Profile.Update")]
    public async Task<IActionResult> RemoveSignatory(Guid id)
    {
        await mediator.Send(new RemoveSignatoryCommand(id));
        return NoContent();
    }
}
