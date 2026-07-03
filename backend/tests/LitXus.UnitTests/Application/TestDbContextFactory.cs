using LitXus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitXus.UnitTests.Application;

/// <summary>
/// Backs the real AppDbContext with EF Core's InMemory provider instead of SQL Server, so
/// Application-layer handler tests can exercise real change-tracking behavior (the exact class of
/// bug that caused the UpdateGLEntryCommandHandler Added-vs-Modified regression) without Docker/
/// Testcontainers, which aren't available in this environment.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
