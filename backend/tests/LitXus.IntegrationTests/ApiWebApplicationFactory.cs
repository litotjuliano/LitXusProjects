using LitXus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LitXus.IntegrationTests;

/// <summary>
/// Boots the real API in-process against a dedicated, throwaway database on the same local SQL
/// Server instance the dev stack already uses (see scripts/run-dev.bat) — Testcontainers/Docker are
/// not available in this environment, and this is otherwise a real HTTP round-trip test host (real
/// EF Core migrations, real ASP.NET Core Identity, real RBAC seeding), not a mocked one.
/// One database per test run, created in InitializeAsync and dropped in DisposeAsync via the
/// collection fixture (see IntegrationTestCollection).
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string DatabaseName { get; } = $"LitXusSystems_IntegrationTest_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"Server=localhost;Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
            });
        });
    }

    /// <summary>Forces the host to boot (migrations + RBAC/demo seeding all run on first client creation).</summary>
    public async Task InitializeAsync()
    {
        using var _ = CreateClient();
        await Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
    }
}
