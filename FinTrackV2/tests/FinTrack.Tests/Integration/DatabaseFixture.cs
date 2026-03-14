using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FinTrack.Tests.Integration;

/// <summary>
/// Starts a real PostgreSQL Docker container once for the entire test run.
/// All integration tests share the same container — fast startup, real SQL.
/// IAsyncLifetime = xUnit's async setup/teardown hook.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("fintrack_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public AppDbContext Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        Db = new AppDbContext(opts);
        await Db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        await _container.DisposeAsync();
    }
}
