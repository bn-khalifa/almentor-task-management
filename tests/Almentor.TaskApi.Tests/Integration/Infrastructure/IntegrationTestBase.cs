using System.Net.Http.Headers;
using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Auth.Dtos;
using Almentor.TaskApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Shared setup for an integration test class: a fresh in-process app, an empty
/// database, and a default registered+authenticated user whose bearer token is
/// already on <see cref="Client"/>. So existing tests keep working unchanged
/// while every request is now authenticated.
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
        await ResetDatabaseAsync();
        Client = _factory.CreateClient();
        await AuthenticateAsync(Client, $"user_{Guid.NewGuid():N}@test.local", "Password123!");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>Mints a second authenticated client (a different user) for ownership-isolation tests.</summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client, email, "Password123!");
        return client;
    }

    /// <summary>A client against the in-memory server with NO auth header — for testing 401s.</summary>
    protected HttpClient CreateUnauthenticatedClient() => _factory.CreateClient();

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = OpenDbContext();
        // Projects first (cascades to Tasks), then Users — FK_Projects_Users_OwnerId is Restrict.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Projects");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Users");
    }

    /// <summary>Direct DB access for asserting side effects the HTTP response alone can't prove (row counts, cascade, DeletedAt).</summary>
    protected AppDbContext OpenDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_fixture.ConnectionString).Options);
}
