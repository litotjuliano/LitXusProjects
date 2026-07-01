using System.Security.Claims;
using LitXus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LitXus.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public Guid? UserId
    {
        get
        {
            var sub = Context?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Context?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Context?.Request.Headers["User-Agent"].ToString();

    public IReadOnlyList<string> Permissions =>
        Context?.User.FindAll("permission").Select(c => c.Value).ToList() ?? [];
}
