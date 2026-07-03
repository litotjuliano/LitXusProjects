namespace LitXus.Infrastructure.Seeding;

/// <summary>Run in dependency order by SeedDatabaseHostedService — see docs/08_Sample_Data.md §8.5.</summary>
public interface ISeeder
{
    int Order { get; }

    /// <summary>
    /// True for seeders that provide reference/lookup data the app cannot function without in any
    /// environment (e.g. the RBAC permission/role catalog) — these run regardless of
    /// Seeding:Enabled. False (default) for demo/sample data that should never appear unannounced
    /// in a production install.
    /// </summary>
    bool AlwaysRun => false;

    Task SeedAsync(CancellationToken cancellationToken);
}
