namespace LitXus.Infrastructure.Seeding;

/// <summary>Run in dependency order by SeedDatabaseHostedService — see docs/08_Sample_Data.md §8.5.</summary>
public interface ISeeder
{
    int Order { get; }
    Task SeedAsync(CancellationToken cancellationToken);
}
