using LitXus.Api.Filters;
using LitXus.Application.Modules.Identity.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/audit-logs")]
public class AdminAuditLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Admin.AuditLogs.Read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(entityName, entityId, userId, action, dateFrom, dateTo));
        return Ok(new { data = result, meta = (object?)null });
    }
}
