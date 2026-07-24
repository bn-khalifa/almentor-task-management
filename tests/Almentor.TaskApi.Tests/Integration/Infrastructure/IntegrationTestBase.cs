using Microsoft.EntityFrameworkCore;
using Almentor.TaskApi.Infrastructure.Persistence;

namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Shared setup for an integration test class: a fresh in-process app (cheap —
/// doesn't restart the container, just a new TestServer) and an empty database
/// before every test, so tests never see another test's leftover data.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private ApiFactory _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(_fixture.ConnectionString);
        Client = _factory.CreateClient();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = OpenDbContext();
        // Deleting Projects cascades to Tasks (FK_Tasks_Projects_ProjectId ON DELETE CASCADE).
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Projects");
    }

    /// <summary>Direct DB access for asserting side effects the HTTP response alone can't prove (row counts, cascade results).</summary>
    protected AppDbContext OpenDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_fixture.ConnectionString).Options);
}
