using System.Net;
using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>
/// The core promise of the auth bonus: "users can only see and manage their own
/// projects and tasks." User A (the base Client) owns data; user B (a second
/// authenticated client) must not see or touch any of it.
/// </summary>
public class OwnershipIsolationFlowTests : IntegrationTestBase
{
    public OwnershipIsolationFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task A_user_cannot_see_or_manage_another_users_project_or_tasks()
    {
        // User A (base Client) creates a project with a task.
        var project = await CreateProject(Client, "A's Project");
        var task = await CreateTask(Client, project, "A's Task");

        // User B — a different authenticated user.
        var clientB = await CreateAuthenticatedClientAsync($"userB_{Guid.NewGuid():N}@test.local");

        // B's project list is empty; A's project is not in it.
        var listB = await clientB.GetAsync("/api/projects");
        var pageB = (await listB.Content.ReadFromJsonAsync<ApiResponse<List<ProjectResponse>>>(TestJson.Options))!;
        pageB.Data!.ShouldBeEmpty();

        // B cannot GET / update / delete A's project (404, not 403 — no existence leak).
        (await clientB.GetAsync($"/api/projects/{project.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientB.PutAsJsonAsync($"/api/projects/{project.Id}",
            new { name = "Hijacked" }, TestJson.Options)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientB.DeleteAsync($"/api/projects/{project.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // B cannot add a task under A's project, nor see/list A's tasks.
        (await clientB.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "sneaky" }, TestJson.Options)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/tasks/{task.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var tasksB = await clientB.GetAsync("/api/tasks");
        var taskPageB = (await tasksB.Content.ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;
        taskPageB.Data!.ShouldBeEmpty();

        // Meanwhile A still sees everything — B's probing changed nothing.
        (await Client.GetAsync($"/api/projects/{project.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync($"/api/tasks/{task.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        clientB.Dispose();
    }

    [Fact]
    public async Task Two_users_may_each_have_a_project_with_the_same_name()
    {
        // Per-owner name uniqueness: "Shared Name" is fine for both.
        await CreateProject(Client, "Shared Name");

        var clientB = await CreateAuthenticatedClientAsync($"userB_{Guid.NewGuid():N}@test.local");
        var response = await clientB.PostAsJsonAsync("/api/projects",
            new { name = "Shared Name" }, TestJson.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        clientB.Dispose();
    }

    private static async Task<ProjectResponse> CreateProject(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name }, TestJson.Options);
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!;
    }

    private static async Task<TaskResponse> CreateTask(HttpClient client, ProjectResponse project, string title)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/tasks", new { title }, TestJson.Options);
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<TaskResponse>>(TestJson.Options))!.Data!;
    }
}
