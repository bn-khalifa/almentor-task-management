using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Domain.Entities;
using Almentor.TaskApi.Domain.Enums;
using Almentor.TaskApi.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Services;

public class TaskServiceTests
{
    private readonly ApplicationServicesFactory _factory = new();

    [Fact]
    public async Task Create_under_a_missing_project_throws_NotFoundException()
    {
        _factory.ProjectRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var request = new CreateTaskRequest { Title = "Orphan task" };

        await Should.ThrowAsync<NotFoundException>(
            () => _factory.TaskService.CreateAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task Create_without_status_or_priority_applies_defaults()
    {
        var project = new Project { Id = Guid.NewGuid(), Name = "Alpha" };
        _factory.ProjectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var request = new CreateTaskRequest { Title = "Untitled work" };

        var response = await _factory.TaskService.CreateAsync(project.Id, request, CancellationToken.None);

        response.Status.ShouldBe(TaskItemStatus.Todo);
        response.Priority.ShouldBe(TaskItemPriority.Medium);
        response.ProjectName.ShouldBe("Alpha");
    }

    [Fact]
    public async Task Create_with_an_explicit_priority_keeps_it_rather_than_defaulting()
    {
        // Guards against the enum-zero-value sentinel trap (Stage 2): Low is the
        // CLR default, so a naive "?? default" implementation could silently
        // swap a real Low for Medium.
        var project = new Project { Id = Guid.NewGuid(), Name = "Alpha" };
        _factory.ProjectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var request = new CreateTaskRequest { Title = "Low priority chore", Priority = TaskItemPriority.Low };

        var response = await _factory.TaskService.CreateAsync(project.Id, request, CancellationToken.None);

        response.Priority.ShouldBe(TaskItemPriority.Low);
    }

    [Fact]
    public async Task Update_from_done_to_todo_is_allowed_and_logs_a_warning()
    {
        var task = MakeTrackedTask(TaskItemStatus.Done);
        _factory.TaskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Status = TaskItemStatus.Todo,
            Priority = TaskItemPriority.Medium
        };

        var response = await _factory.TaskService.UpdateAsync(task.Id, request, CancellationToken.None);

        // Allowed: no exception, and the new status is actually persisted.
        response.Status.ShouldBe(TaskItemStatus.Todo);
        // Logged: the unusual transition is visible, not silent.
        _factory.TaskServiceLogger.Entries.ShouldContain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains("Done back to Todo"));
    }

    [Fact]
    public async Task Update_between_other_statuses_does_not_log_a_warning()
    {
        var task = MakeTrackedTask(TaskItemStatus.Todo);
        _factory.TaskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var request = new UpdateTaskRequest
        {
            Title = task.Title,
            Status = TaskItemStatus.InProgress,
            Priority = TaskItemPriority.Medium
        };

        await _factory.TaskService.UpdateAsync(task.Id, request, CancellationToken.None);

        _factory.TaskServiceLogger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Listing_tasks_for_a_missing_project_throws_NotFoundException()
    {
        _factory.ProjectRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _factory.TaskService.GetPagedAsync(Guid.NewGuid(), new TaskQueryParameters(), CancellationToken.None));
    }

    private static TaskItem MakeTrackedTask(TaskItemStatus status)
    {
        var project = new Project { Id = Guid.NewGuid(), Name = "Alpha" };
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project, // TaskResponse mapping reads Project.Name; must be non-null.
            Title = "Ship it",
            Status = status,
            Priority = TaskItemPriority.Medium
        };
    }
}
