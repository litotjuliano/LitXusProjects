using LitXus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LitXus.Api.Filters;

/// <summary>See docs/06_RBAC_Auth.md §6.5 — is this module licensed and currently enabled?</summary>
public class RequireModuleAttribute(Module module) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var featureFlagService = context.HttpContext.RequestServices.GetRequiredService<IFeatureFlagService>();

        if (!featureFlagService.IsEnabled(module))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                error = new { code = "MODULE_NOT_ENABLED", message = $"The {module} module is not enabled for this installation." },
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}
