using LitXus.Api.Filters;
using LitXus.Application.Modules.Identity.Commands.AssignRole;
using LitXus.Application.Modules.Identity.Commands.RevokeRole;
using LitXus.Application.Modules.Identity.Commands.UpdateUserStatus;
using LitXus.Application.Modules.Identity.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record UpdateUserStatusRequest(bool IsActive);
public record AssignRoleRequest(Guid RoleId);

[ApiController]
[Authorize]
[Route("api/v1/admin/users")]
public class AdminUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Admin.Users.Read")]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetUsersQuery());
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission("Admin.Users.Update")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        await mediator.Send(new UpdateUserStatusCommand(id, request.IsActive));
        return NoContent();
    }

    [HttpPost("{id:guid}/roles")]
    [RequirePermission("Admin.Users.Update")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        await mediator.Send(new AssignRoleCommand(id, request.RoleId));
        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [RequirePermission("Admin.Users.Update")]
    public async Task<IActionResult> RevokeRole(Guid id, Guid roleId)
    {
        await mediator.Send(new RevokeRoleCommand(id, roleId));
        return NoContent();
    }
}
