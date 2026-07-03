using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Runs seeders in Order on startup. Demo/sample seeders are gated by Seeding:Enabled — on for
/// Local/Demo, off by default in Production, so a production install never silently gets demo
/// data. Seeders with AlwaysRun=true (currently only RbacSeeder) run regardless — they provide
/// reference data the app cannot function without in any environment. See docs/08_Sample_Data.md
/// §8.5.
///
/// When Seeding:Enabled is false, only RbacSeeder is resolved directly (not via
/// IEnumerable&lt;ISeeder&gt;) — DI's IEnumerable&lt;T&gt; resolution constructs every registered
/// implementation up front, not lazily, so enumerating ISeeder would eagerly construct every
/// demo-data seeder's dependencies too (e.g. LicenseSeeder needs ILicenseKeyVerifier, which
/// throws if Licensing:PublicKeyPem isn't configured — exactly the state a brand-new production
/// install is in before its first license key is applied).
/// </summary>
public class SeedDatabaseHostedService(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        if (configuration.GetValue<bool>("Seeding:Enabled"))
        {
            var seeders = scope.ServiceProvider.GetServices<ISeeder>().OrderBy(s => s.Order);
            foreach (var seeder in seeders)
            {
                await seeder.SeedAsync(cancellationToken);
            }
        }
        else
        {
            await scope.ServiceProvider.GetRequiredService<RbacSeeder>().SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
