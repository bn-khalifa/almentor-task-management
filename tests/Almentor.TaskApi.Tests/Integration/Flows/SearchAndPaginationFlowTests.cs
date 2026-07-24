using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>Spec flow 3: Search tasks and verify pagination.</summary>
public class SearchAndPaginationFlowTests : IntegrationTestBase
{
    public SearchAndPaginationFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Search_matches_partial_and_is_case_insensitive()
    {
        var projectId = await CreateProject("Search Flow");

        await CreateTask(projectId, "Write API documentation");
        await CreateTask(projectId, "Review pull request");
        await CreateTask(projectId, "Update deployment docs");

        var response = await Client.GetAsync("/api/tasks?q=DOCUMENT");
        var page = (await response.Content
            .ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;

        // Matches "documentation" (title) case-insensitively; "docs" does not
        // contain the substring "document", so it correctly does not match.
        page.Data!.Count.ShouldBe(1);
        page.Data![0].Title.ShouldBe("Write API documentation");
    }

    [Fact]
    public async Task Pagination_returns_the_requested_page_size_while_reporting_the_full_total()
    {
        var projectId = await CreateProject("Pagination Flow");

        for (var i = 1; i <= 5; i++)
        {
            await CreateTask(projectId, $"Task {i}");
        }

        var firstPage = await GetPage(limit: 2, offset: 0);
        firstPage.Data!.Count.ShouldBe(2);
        firstPage.Meta!.Pagination!.Total.ShouldBe(5);
        firstPage.Meta!.Pagination!.Limit.ShouldBe(2);
        firstPage.Meta!.Pagination!.Offset.ShouldBe(0);

        var secondPage = await GetPage(limit: 2, offset: 2);
        secondPage.Data!.Count.ShouldBe(2);
        secondPage.Meta!.Pagination!.Total.ShouldBe(5);

        var thirdPage = await GetPage(limit: 2, offset: 4);
        thirdPage.Data!.Count.ShouldBe(1); // only 1 item left on the last page
        thirdPage.Meta!.Pagination!.Total.ShouldBe(5);

        // No overlap and no gaps: every id appears on exactly one page.
        var allIds = firstPage.Data!.Concat(secondPage.Data!).Concat(thirdPage.Data!)
            .Select(t => t.Id).ToList();
        allIds.Distinct().Count().ShouldBe(5);
    }

    private async Task<Guid> CreateProject(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name }, TestJson.Options);
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<ProjectResponse>>(TestJson.Options))!.Data!.Id;
    }

    private async Task CreateTask(Guid projectId, string title) =>
        await Client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title }, TestJson.Options);

    private async Task<ApiResponse<List<TaskResponse>>> GetPage(int limit, int offset)
    {
        var response = await Client.GetAsync($"/api/tasks?limit={limit}&offset={offset}&sort=created_at&direction=asc");
        return (await response.Content
            .ReadFromJsonAsync<ApiResponse<List<TaskResponse>>>(TestJson.Options))!;
    }
}
