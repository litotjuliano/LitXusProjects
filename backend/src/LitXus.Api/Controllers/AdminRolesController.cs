using LitXus.Api.Filters;
using LitXus.Application.Modules.Identity.Queries.GetPermissions;
using LitXus.Application.Modules.Identity.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
public class AdminRolesController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/v1/admin/roles")]
    [RequirePermission("Admin.Roles.Read")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await mediator.Send(new GetRolesQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("api/v1/admin/permissions")]
    [RequirePermission("Admin.Roles.Read")]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await mediator.Send(new GetPermissionsQuery());
        return Ok(new { data = result, meta = (object?)null });
    }
}
