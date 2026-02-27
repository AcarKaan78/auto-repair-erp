using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Tests.Helpers;

public static class TestDbContextFactory
{
    /// <summary>
    /// Creates an in-memory SQLite context. Use for CRUD tests.
    /// Note: SQLite doesn't support Sum on decimal — use CreateInMemory for aggregate tests.
    /// </summary>
    public static (AppDbContext context, SqliteConnection connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    /// <summary>
    /// Creates an EF Core InMemory context. Use for tests that require decimal aggregation (Sum, Avg).
    /// </summary>
    public static AppDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }
}
