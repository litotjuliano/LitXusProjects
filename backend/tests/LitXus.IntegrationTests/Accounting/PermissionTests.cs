using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Accounting;

[Collection("Integration")]
public class PermissionTests(ApiWebApplicationFactory factory)
{
    // Viewer is seeded with only *.Read permissions (RbacSeeder) — no Create/Update/Approve anywhere.
    [Fact]
    public async Task CreateAccount_AsViewer_Returns403()
    {
        var client = await factory.AuthenticatedClientAsync("viewer@litxus.demo");

        var response = await client.PostAsJsonAsync("/api/v1/accounting/accounts", new
        {
            code = $"9{Random.Shared.Next(100, 999)}",
            name = "Should Be Forbidden",
            type = "Asset",
            parentAccountId = (Guid?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateGLEntry_AsViewer_Returns403()
    {
        var client = await factory.AuthenticatedClientAsync("viewer@litxus.demo");

        var response = await client.PostAsJsonAsync("/api/v1/accounting/gl-entries", new
        {
            entryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            description = "Should be forbidden",
            lines = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAccounts_AsViewer_Returns200()
    {
        var client = await factory.AuthenticatedClientAsync("viewer@litxus.demo");

        var response = await client.GetAsync("/api/v1/accounting/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
