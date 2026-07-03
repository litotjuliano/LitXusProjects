using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Accounting;

[Collection("Integration")]
public class GLEntryLifecycleTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record AccountResponse(Guid Id);
    private record GLEntryResponse(Guid Id, string? EntryNumber, string Status, string? VoidReason);

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string type)
    {
        var code = $"9{Random.Shared.Next(10000, 99999)}";
        var response = await client.PostAsJsonAsync("/api/v1/accounting/accounts", new
        {
            code,
            name = $"GL Test {type} Account",
            type,
            parentAccountId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<AccountResponse>>(AuthHelper.JsonOptions);
        return body!.Data.Id;
    }

    [Fact]
    public async Task CreatePostVoid_FullLifecycle_Succeeds()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var cashAccountId = await CreateAccountAsync(client, "Asset");
        var revenueAccountId = await CreateAccountAsync(client, "Revenue");

        // Create (Draft)
        var createResponse = await client.PostAsJsonAsync("/api/v1/accounting/gl-entries", new
        {
            entryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            description = "Integration test entry",
            lines = new[]
            {
                new { accountId = cashAccountId, debitAmount = 500m, creditAmount = 0m, lineDescription = (string?)null },
                new { accountId = revenueAccountId, debitAmount = 0m, creditAmount = 500m, lineDescription = (string?)null },
            },
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Envelope<GLEntryResponse>>(AuthHelper.JsonOptions);
        created!.Data.Status.Should().Be("Draft");
        created.Data.EntryNumber.Should().BeNull();
        var entryId = created.Data.Id;

        // Post
        var postResponse = await client.PostAsync($"/api/v1/accounting/gl-entries/{entryId}/post", null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var posted = await postResponse.Content.ReadFromJsonAsync<Envelope<GLEntryResponse>>(AuthHelper.JsonOptions);
        posted!.Data.Status.Should().Be("Posted");
        posted.Data.EntryNumber.Should().NotBeNullOrEmpty();

        // Void
        var voidResponse = await client.PostAsJsonAsync($"/api/v1/accounting/gl-entries/{entryId}/void", new { reason = "Integration test cleanup" });
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var voided = await voidResponse.Content.ReadFromJsonAsync<Envelope<GLEntryResponse>>(AuthHelper.JsonOptions);
        voided!.Data.Status.Should().Be("Voided");
        voided.Data.VoidReason.Should().Be("Integration test cleanup");
    }

    [Fact]
    public async Task Post_WhenUnbalanced_Returns422()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var cashAccountId = await CreateAccountAsync(client, "Asset");
        var revenueAccountId = await CreateAccountAsync(client, "Revenue");

        var createResponse = await client.PostAsJsonAsync("/api/v1/accounting/gl-entries", new
        {
            entryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            description = "Unbalanced integration test entry",
            lines = new[]
            {
                new { accountId = cashAccountId, debitAmount = 100m, creditAmount = 0m, lineDescription = (string?)null },
                new { accountId = revenueAccountId, debitAmount = 0m, creditAmount = 90m, lineDescription = (string?)null },
            },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Envelope<GLEntryResponse>>(AuthHelper.JsonOptions);

        var postResponse = await client.PostAsync($"/api/v1/accounting/gl-entries/{created!.Data.Id}/post", null);

        postResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
