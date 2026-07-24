using Almentor.TaskApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Almentor.TaskApi.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real app (real DI wiring, real middleware, real controllers) with
/// one substitution: the AppDbContext points at the Testcontainers SQL Server
/// instead of whatever connection string Program.cs would otherwise resolve.
/// Removes and re-adds the DbContextOptions descriptor — the standard, robust
/// pattern for overriding EF Core registrations in WebApplicationFactory, since
/// it runs after (and so overrides) Program.cs's own AddInfrastructure call.
/// </summary>
public class ApiFactory : WebApplicationFactory<global::Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString)
    {
        _connectionString = connectionString;

        // Program.cs reads ConnectionStrings:DefaultConnection and Jwt:Key
        // EAGERLY — as its own top-level statements execute, before Build() is
        // ever reached — and throws/fails validation if either is empty. With
        // ASPNETCORE_ENVIRONMENT=Testing (not Development), user-secrets aren't
        // loaded, so both would be empty. ConfigureAppConfiguration below can't
        // fix this: WebApplicationFactory only merges those additions in at
        // Build() time, which is AFTER Program.cs's own eager reads already ran.
        // Environment variables, by contrast, are read synchronously by
        // WebApplication.CreateBuilder itself — the very first thing Program.cs
        // does — so setting them on the process here, before the host boots,
        // reliably satisfies both guards. ConfigureServices below then swaps in
        // the real Testcontainers connection string afterward.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=placeholder;Database=placeholder;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "integration-test-only-signing-key-not-a-real-secret-32chars");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" tells Program.cs to skip its startup auto-seed — each test
        // manages its own known dataset via SqlServerFixture/IntegrationTestBase,
        // so seeding sample data on top of that would just be noise.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_connectionString, sql =>
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));
        });
    }
}
