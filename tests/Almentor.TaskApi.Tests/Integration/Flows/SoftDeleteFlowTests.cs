using System.Net;
using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>
/// Verifies the Soft Deletes bonus: deletes retain rows with DeletedAt set,
/// stay invisible to the API, cascade from project to tasks, and free the
/// project name for reuse. Uses IgnoreQueryFilters() to look "behind" the
/// global filter and prove the rows are really still there.
/// </summary>
public class SoftDeleteFlowTests : IntegrationTestBase
{
    public SoftDeleteFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Deleting_a_task_soft_deletes_it_row_persists_with_DeletedAt_set()
    {
        var projectId = await CreateProject("Soft Task");
        var taskId = await CreateTask(projectId, "Disposable");

        var deleteResponse = await Client.DeleteAsync($"/api/tasks/{taskId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Invisible to the API...
        (await Client.GetAsync($"/api/tasks/{taskId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // ...but the row physically remains, now stamped with DeletedAt.
        await using var db = OpenDbContext();
        (await db.Tasks.CountAsync()).ShouldBe(0); // hidden by the global filter
        var raw = await db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.Id == taskId);
        raw.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Deleting_a_project_cascade_soft_deletes_its_tasks()
    {
        var projectId = await CreateProject("Soft Project");
        await CreateTask(projectId, "Task 1");
        await CreateTask(projectId, "Task 2");

        var deleteResponse = await Client.DeleteAsync($"/api/projects/{projectId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Project and its tasks are all gone from the API surface.
        (await Client.GetAsync($"/api/projects/{projectId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var listResponse = await Client.GetAsync("/api/tasks");
        var page = (await listResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;
        page.Data!.ShouldBeEmpty();

        // But at the DB level, everything persists with DeletedAt stamped — the
        // cascade soft-deleted the tasks, it did not hard-delete anything.
        await using var db = OpenDbContext();
        (await db.Projects.IgnoreQueryFilters().CountAsync(p => p.Id == projectId)).ShouldBe(1);
        var tasks = await db.Tasks.IgnoreQueryFilters().Where(t => t.ProjectId == projectId).ToListAsync();
        tasks.Count.ShouldBe(2);
        tasks.ShouldAllBe(t => t.DeletedAt != null);
    }

    [Fact]
    public async Task A_soft_deleted_project_name_can_be_reused()
    {
        var firstId = await CreateProject("Reusable Name");

        var deleteResponse = await Client.DeleteAsync($"/api/projects/{firstId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The filtered unique index no longer counts the deleted row, so the
        // same name is accepted for a brand-new project.
        var secondResponse = await Client.PostAsJsonAsync(
            "/api/projects", new { name = "Reusable Name" }, TestJson.Options);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = (await secondResponse.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!;
        second.Id.ShouldNotBe(firstId);
    }

    private async Task<Guid> CreateProject(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name }, TestJson.Options);
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!.Id;
    }

    private async Task<Guid> CreateTask(Guid projectId, string title)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks", new { title }, TestJson.Options);
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<TaskResponse>>(TestJson.Options))!.Data!.Id;
    }
}
