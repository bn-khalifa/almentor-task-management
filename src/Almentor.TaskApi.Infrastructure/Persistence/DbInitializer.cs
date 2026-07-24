using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Domain.Entities;
using Almentor.TaskApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Almentor.TaskApi.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations on startup and, if the database has no users yet,
/// populates it with sample data so a fresh `docker compose up` yields a working,
/// browsable API. Idempotent: seeding is skipped once any user exists.
/// </summary>
public class DbInitializer
{
    // Shared demo password for every seeded account.
    public const string SeedPassword = "Password123!";

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext context, IPasswordHasher passwordHasher, ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(bool seed, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying database migrations...");
        await _context.Database.MigrateAsync(ct);

        if (!seed)
        {
            return;
        }

        if (await _context.Users.AnyAsync(ct))
        {
            _logger.LogInformation("Database already has users; skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding sample data...");
        await SeedAsync(ct);
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hash = _passwordHasher.Hash(SeedPassword);

        var alice = new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = hash };
        var bob = new User { Id = Guid.NewGuid(), Email = "bob@example.com", PasswordHash = hash };

        var redesign = new Project
        {
            Id = Guid.NewGuid(), OwnerId = alice.Id, Name = "Website Redesign", Description = "Q3 marketing site refresh",
            Tasks =
            [
                Task_("Audit current pages", TaskItemStatus.Done, TaskItemPriority.Medium, null),
                Task_("Design new landing page", TaskItemStatus.InProgress, TaskItemPriority.High, today.AddDays(7)),
                Task_("Implement responsive nav", TaskItemStatus.Todo, TaskItemPriority.High, today.AddDays(14)),
                Task_("Write copy", TaskItemStatus.Todo, TaskItemPriority.Low, today.AddDays(21))
            ]
        };

        var mobile = new Project
        {
            Id = Guid.NewGuid(), OwnerId = alice.Id, Name = "Mobile App", Description = "iOS/Android companion app",
            Tasks =
            [
                Task_("Set up CI pipeline", TaskItemStatus.Done, TaskItemPriority.Medium, null),
                Task_("Build login screen", TaskItemStatus.InProgress, TaskItemPriority.Medium, today.AddDays(10))
            ]
        };

        var campaign = new Project
        {
            Id = Guid.NewGuid(), OwnerId = bob.Id, Name = "Marketing Campaign", Description = "Autumn launch",
            Tasks =
            [
                Task_("Draft email sequence", TaskItemStatus.Todo, TaskItemPriority.High, today.AddDays(3)),
                Task_("Book ad slots", TaskItemStatus.Todo, TaskItemPriority.Medium, today.AddDays(5))
            ]
        };

        await _context.Users.AddRangeAsync([alice, bob], ct);
        await _context.Projects.AddRangeAsync([redesign, mobile, campaign], ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Seeded {Users} users and {Projects} projects. Login with any seeded email and password '{Password}'.",
            2, 3, SeedPassword);
    }

    private static TaskItem Task_(string title, TaskItemStatus status, TaskItemPriority priority, DateOnly? due) =>
        new() { Id = Guid.NewGuid(), Title = title, Status = status, Priority = priority, DueDate = due };
}
