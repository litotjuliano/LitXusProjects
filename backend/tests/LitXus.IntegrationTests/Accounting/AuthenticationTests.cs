using System.Net;
using FluentAssertions;

namespace LitXus.IntegrationTests.Accounting;

[Collection("Integration")]
public class AuthenticationTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task GetAccounts_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/accounting/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_WithMalformedToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync("/api/v1/accounting/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
