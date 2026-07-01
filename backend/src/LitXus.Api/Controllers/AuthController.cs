using System.Security.Claims;
using LitXus.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IdentityService identityService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await identityService.RegisterAsync(request.Email, request.Password, request.FullName);
            return StatusCode(StatusCodes.Status201Created, new { data = new { user.Id, user.Email, user.IsActive } });
        }
        catch (AuthenticationException ex)
        {
            return BadRequest(new { error = new { code = "REGISTRATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await identityService.LoginAsync(request.Email, request.Password);
            return Ok(new { data = result });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { error = new { code = "USER_NOT_ACTIVE", message = ex.Message } });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await identityService.RefreshAsync(request.RefreshToken);
            return Ok(new { data = result });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { error = new { code = "INVALID_REFRESH_TOKEN", message = ex.Message } });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await identityService.LogoutAsync(userId);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var session = await identityService.GetSessionAsync(userId);
        return Ok(new { data = session });
    }
}
