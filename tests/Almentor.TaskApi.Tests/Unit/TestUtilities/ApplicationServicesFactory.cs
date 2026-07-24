using Almentor.TaskApi.Application;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Features.Projects;
using Almentor.TaskApi.Application.Features.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Almentor.TaskApi.Tests.Unit.TestUtilities;

/// <summary>
/// Wires up the real Application-layer composition root (<c>AddApplication()</c>)
/// — the actual Mapster config and FluentValidation validators, not hand-rolled
/// substitutes for them — while substituting only the true external boundary:
/// the repositories and the clock. This means a service test exercises the same
/// validation and mapping wiring production uses, isolating just the database.
/// </summary>
public class ApplicationServicesFactory
{
    public IProjectRepository ProjectRepository { get; } = Substitute.For<IProjectRepository>();
    public ITaskRepository TaskRepository { get; } = Substitute.For<ITaskRepository>();
    public FakeDateTimeProvider Clock { get; init; } = new(new DateOnly(2026, 7, 24));
    public TestLogger<TaskService> TaskServiceLogger { get; } = new();
    public FakeCurrentUserService CurrentUser { get; } = new();

    /// <summary>The id services will stamp on / check against — expose it so tests can match owned entities.</summary>
    public Guid CurrentUserId => CurrentUser.UserId;

    private readonly Lazy<ServiceProvider> _provider;

    public ApplicationServicesFactory()
    {
        _provider = new Lazy<ServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddSingleton<IDateTimeProvider>(Clock);
            services.AddSingleton<ICurrentUserService>(CurrentUser);
            services.AddScoped(_ => ProjectRepository);
            services.AddScoped(_ => TaskRepository);
            // Registered after AddApplication/AddLogging so it wins resolution
            // for this specific closed generic type.
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<TaskService>>(TaskServiceLogger);
            return services.BuildServiceProvider();
        });
    }

    public IProjectService ProjectService => _provider.Value.GetRequiredService<IProjectService>();
    public ITaskService TaskService => _provider.Value.GetRequiredService<ITaskService>();
}
