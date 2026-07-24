using Almentor.TaskApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
