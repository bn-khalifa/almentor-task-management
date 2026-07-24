using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Infrastructure.Auth;
using Almentor.TaskApi.Infrastructure.Persistence;
using Almentor.TaskApi.Infrastructure.Persistence.Repositories;
using Almentor.TaskApi.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Auth: bind + validate JwtSettings (fail fast at startup if the signing
        // key is missing), plus the password hasher and token generator.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Key) && s.Key.Length >= 32,
                "Jwt:Key must be configured and at least 32 characters. " +
                "Set it via user-secrets (dev) or the Jwt__Key environment variable (Docker).")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
