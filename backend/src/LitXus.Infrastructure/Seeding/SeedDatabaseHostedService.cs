using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Runs seeders in Order on startup, gated by Seeding:Enabled — on for Local/Demo, off by default
/// in Production, so a production install never silently gets demo data. See docs/08_Sample_Data.md §8.5.
/// </summary>
public class SeedDatabaseHostedService(IServiceProvider serviceProvider, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Seeding:Enabled"))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>().OrderBy(s => s.Order);

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
