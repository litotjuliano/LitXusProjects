using System.Net;
using FluentAssertions;
using LitXus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LitXus.IntegrationTests.Accounting;

[Collection("Integration")]
public class ModuleGatingTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task GetAccounts_WhenAccountingModuleNotLicensed_Returns403()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");

        // IFeatureFlagService is Scoped (reads the Licenses row fresh per HTTP request, no
        // cross-request cache to invalidate) — so directly persisting the change is enough for
        // the *next* request to see it. Restored in `finally` since this mutates DB state shared
        // by every other test in this collection (xUnit runs test classes within one collection
        // sequentially, so this is safe as long as the license is put back before returning).
        await SetEnabledModulesAsync([]);
        try
        {
            var response = await client.GetAsync("/api/v1/accounting/accounts");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("MODULE_NOT_ENABLED");
        }
        finally
        {
            await SetEnabledModulesAsync(["Accounting"]);
        }
    }

    private async Task SetEnabledModulesAsync(IReadOnlyList<string> modules)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var license = await db.Licenses.OrderByDescending(l => l.IssuedAtUtc).FirstAsync();
        license.ApplyVerifiedKey(
            license.ProductCode, license.IssuedToCompany, modules, license.IssuedAtUtc, license.ExpiresAtUtc, license.LicenseKey);
        await db.SaveChangesAsync();
    }
}
