using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LitXus.IntegrationTests;

public static class AuthHelper
{
    public const string DemoPassword = "Demo@12345";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record LoginEnvelope(LoginData Data);
    private record LoginData(string AccessToken);

    /// <summary>Logs in as one of the seeded demo accounts and returns an HttpClient with the Bearer token attached.</summary>
    public static async Task<HttpClient> AuthenticatedClientAsync(this ApiWebApplicationFactory factory, string email, string password = DemoPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginEnvelope>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data.AccessToken);
        return client;
    }
}
