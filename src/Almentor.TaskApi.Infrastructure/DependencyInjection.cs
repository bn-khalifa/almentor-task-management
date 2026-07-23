using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Infrastructure.Persistence;
using Almentor.TaskApi.Infrastructure.Persistence.Repositories;
using Almentor.TaskApi.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Almentor.TaskApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Fail fast with a clear message rather than a vague error at first query.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set it via user-secrets (local dev) or the " +
                "ConnectionStrings__DefaultConnection environment variable (Docker).");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                // Keep migrations in this (Infrastructure) assembly, not the API.
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
