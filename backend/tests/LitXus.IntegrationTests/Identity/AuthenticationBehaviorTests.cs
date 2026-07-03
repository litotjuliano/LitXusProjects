using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LitXus.Infrastructure.Identity;
using LitXus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LitXus.IntegrationTests.Identity;

/// <summary>
/// Regression coverage for 3 auth behaviors that were already implemented in
/// IdentityService but had no automated test locking them in (found unchecked in
/// Features.md, then verified live before writing these — see docs/phase-1-accounting/Features.md).
/// </summary>
[Collection("Integration")]
public class AuthenticationBehaviorTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record LoginData(string AccessToken, string RefreshToken);

    [Fact]
    public async Task Login_WhenAccountDeactivated_Returns401WithClearMessage()
    {
        await SetAccountantActiveAsync(false);
        try
        {
            var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "accountant@litxus.demo", password = AuthHelper.DemoPassword });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("USER_NOT_ACTIVE");
        }
        finally
        {
            await SetAccountantActiveAsync(true);
        }
    }

    [Fact]
    public async Task RefreshToken_RotatesOnUse_OldTokenIsRejectedAfterwards()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "viewer@litxus.demo", password = AuthHelper.DemoPassword });
        var loginBody = await login.Content.ReadFromJsonAsync<Envelope<LoginData>>(AuthHelper.JsonOptions);
        var originalRefreshToken = loginBody!.Data.RefreshToken;

        var firstRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = originalRefreshToken });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstRefreshBody = await firstRefresh.Content.ReadFromJsonAsync<Envelope<LoginData>>(AuthHelper.JsonOptions);
        firstRefreshBody!.Data.RefreshToken.Should().NotBe(originalRefreshToken);

        var reuseOldToken = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = originalRefreshToken });

        reuseOldToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "viewer@litxus.demo", password = AuthHelper.DemoPassword });
        var loginBody = await login.Content.ReadFromJsonAsync<Envelope<LoginData>>(AuthHelper.JsonOptions);
        var (accessToken, refreshToken) = (loginBody!.Data.AccessToken, loginBody.Data.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SetAccountantActiveAsync(bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == "accountant@litxus.demo");
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }
}
