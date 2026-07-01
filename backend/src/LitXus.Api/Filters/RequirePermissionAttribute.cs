using Microsoft.AspNetCore.Mvc.Filters;

namespace LitXus.Api.Filters;

/// <summary>
/// Reads the "permission" claims embedded in the JWT (docs/06_RBAC_Auth.md §6.3) — no DB round-trip
/// per request. Deliberately generic error message so a probing client can't enumerate the permission model.
/// </summary>
public class RequirePermissionAttribute(string permissionCode) : Attribute, IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasPermission = context.HttpContext.User.Claims
            .Any(c => c.Type == "permission" && c.Value == permissionCode);

        if (!hasPermission)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                error = new { code = "FORBIDDEN", message = "You do not have permission to perform this action." },
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return Task.CompletedTask;
        }

        return next();
    }
}
