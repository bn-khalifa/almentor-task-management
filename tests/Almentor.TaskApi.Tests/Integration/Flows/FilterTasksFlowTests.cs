using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Domain.Enums;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>Spec flow 2: Filter tasks by status and priority.</summary>
public class FilterTasksFlowTests : IntegrationTestBase
{
    public FilterTasksFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Filtering_by_status_and_priority_returns_exactly_the_matching_subset()
    {
        var projectResponse = await Client.PostAsJsonAsync(
            "/api/projects", new { name = "Filter Flow" }, TestJson.Options);
        var projectId = (await projectResponse.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!.Id;

        // A mixed set: only one task is both status=todo AND priority=high.
        await CreateTask(projectId, "Todo/High", status: "todo", priority: "high");
        await CreateTask(projectId, "Todo/Low", status: "todo", priority: "low");
        await CreateTask(projectId, "Done/High", status: "done", priority: "high");
        await CreateTask(projectId, "InProgress/Medium", status: "in_progress", priority: "medium");

        var response = await Client.GetAsync("/api/tasks?status=todo&priority=high");
        var page = (await response.Content
            .ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;

        page.Data!.Count.ShouldBe(1);
        page.Data![0].Title.ShouldBe("Todo/High");
        page.Data![0].Status.ShouldBe(TaskItemStatus.Todo);
        page.Data![0].Priority.ShouldBe(TaskItemPriority.High);
        page.Meta!.Pagination!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task Filtering_by_status_alone_returns_every_task_in_that_status()
    {
        var projectResponse = await Client.PostAsJsonAsync(
            "/api/projects", new { name = "Filter Flow 2" }, TestJson.Options);
        var projectId = (await projectResponse.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!.Id;

        await CreateTask(projectId, "Todo A", status: "todo", priority: "low");
        await CreateTask(projectId, "Todo B", status: "todo", priority: "high");
        await CreateTask(projectId, "Done A", status: "done", priority: "medium");

        var response = await Client.GetAsync("/api/tasks?status=todo");
        var page = (await response.Content
            .ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;

        page.Data!.Count.ShouldBe(2);
        page.Data!.ShouldAllBe(t => t.Status == TaskItemStatus.Todo);
    }

    private async Task CreateTask(Guid projectId, string title, string status, string priority) =>
        await Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks",
            new { title, status, priority },
            TestJson.Options);
}
