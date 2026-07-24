using Almentor.TaskApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Starts a real, throwaway SQL Server 2022 container once per test run and
/// applies the actual EF Core migrations to it — so integration tests exercise
/// the real generated SQL, unique index, check constraints, cascade delete, and
/// collation-based LIKE behavior, not an in-memory or SQLite substitute that
/// would give false confidence on exactly what we most want to prove.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
