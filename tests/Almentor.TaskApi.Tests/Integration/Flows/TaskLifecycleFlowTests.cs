using System.Net;
using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Domain.Enums;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>
/// Spec flow 1: Create project → Add task → Mark task as done → Delete project.
/// Asserts both the HTTP responses along the way AND the actual database state
/// afterward — not just that the API said "204", but that the rows are really gone.
/// </summary>
public class TaskLifecycleFlowTests : IntegrationTestBase
{
    public TaskLifecycleFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Create_project_add_task_mark_done_then_delete_project_removes_everything()
    {
        // Create project
        var createProjectResponse = await Client.PostAsJsonAsync(
            "/api/projects", new { name = "Integration Flow" }, TestJson.Options);
        createProjectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var project = (await createProjectResponse.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!;

        // Add task
        var createTaskResponse = await Client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/tasks", new { title = "Do the thing" }, TestJson.Options);
        createTaskResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = (await createTaskResponse.Content
            .ReadFromJsonAsync<ApiResponse<TaskResponse>>(TestJson.Options))!.Data!;
        task.Status.ShouldBe(TaskItemStatus.Todo);
        task.ProjectName.ShouldBe("Integration Flow");

        // Mark task as done
        var markDoneResponse = await Client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new { title = task.Title, status = "done", priority = "medium" },
            TestJson.Options);
        markDoneResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedTask = (await markDoneResponse.Content
            .ReadFromJsonAsync<ApiResponse<TaskResponse>>(TestJson.Options))!.Data!;
        updatedTask.Status.ShouldBe(TaskItemStatus.Done);

        // Delete project
        var deleteResponse = await Client.DeleteAsync($"/api/projects/{project.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The task is gone via the API too (not just the project)
        var getTaskResponse = await Client.GetAsync($"/api/tasks/{task.Id}");
        getTaskResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Side effect at the DB level: cascade actually removed the row, not
        // just something the response body claims.
        await using var db = OpenDbContext();
        (await db.Projects.CountAsync()).ShouldBe(0);
        (await db.Tasks.CountAsync()).ShouldBe(0);
    }
}
