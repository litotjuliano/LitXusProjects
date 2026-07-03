using System.Security.Claims;
using FluentAssertions;
using LitXus.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LitXus.UnitTests.Api;

public class RequirePermissionAttributeTests
{
    private static ActionExecutingContext BuildContext(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserHasPermissionClaim_CallsNext()
    {
        var context = BuildContext(new Claim("permission", "Accounting.GLEntry.Approve"));
        var attribute = new RequirePermissionAttribute("Accounting.GLEntry.Approve");
        var nextCalled = false;

        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserLacksPermissionClaim_Returns403WithoutCallingNext()
    {
        var context = BuildContext(new Claim("permission", "Accounting.GLEntry.Read"));
        var attribute = new RequirePermissionAttribute("Accounting.GLEntry.Approve");
        var nextCalled = false;

        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
