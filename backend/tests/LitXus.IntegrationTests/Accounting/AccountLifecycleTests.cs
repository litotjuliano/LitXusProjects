using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Accounting;

[Collection("Integration")]
public class AccountLifecycleTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record AccountResponse(Guid Id, string Code, string Name, string Type, bool IsActive);

    [Fact]
    public async Task CreateUpdateDeactivateReactivate_FullLifecycle_Succeeds()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var code = $"9{Random.Shared.Next(1000, 9999)}";

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/v1/accounting/accounts", new
        {
            code,
            name = "Integration Test Account",
            type = "Asset",
            parentAccountId = (Guid?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Envelope<AccountResponse>>(AuthHelper.JsonOptions);
        created!.Data.Code.Should().Be(code);
        created.Data.IsActive.Should().BeTrue();
        var accountId = created.Data.Id;

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/accounting/accounts/{accountId}", new
        {
            name = "Renamed Integration Test Account",
            parentAccountId = (Guid?)null,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<Envelope<AccountResponse>>(AuthHelper.JsonOptions);
        updated!.Data.Name.Should().Be("Renamed Integration Test Account");
        updated.Data.Code.Should().Be(code); // immutable

        // Deactivate
        var deactivateResponse = await client.PostAsync($"/api/v1/accounting/accounts/{accountId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDeactivate = await client.GetFromJsonAsync<Envelope<List<AccountResponse>>>(
            "/api/v1/accounting/accounts?includeInactive=true", AuthHelper.JsonOptions);
        afterDeactivate!.Data.Single(a => a.Id == accountId).IsActive.Should().BeFalse();

        // Reactivate
        var reactivateResponse = await client.PostAsync($"/api/v1/accounting/accounts/{accountId}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterReactivate = await client.GetFromJsonAsync<Envelope<List<AccountResponse>>>(
            "/api/v1/accounting/accounts?includeInactive=true", AuthHelper.JsonOptions);
        afterReactivate!.Data.Single(a => a.Id == accountId).IsActive.Should().BeTrue();
    }
}
