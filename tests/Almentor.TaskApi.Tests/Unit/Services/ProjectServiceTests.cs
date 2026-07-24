using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Domain.Entities;
using Almentor.TaskApi.Tests.Unit.TestUtilities;
using NSubstitute;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Services;

public class ProjectServiceTests
{
    private readonly ApplicationServicesFactory _factory = new();

    [Fact]
    public async Task Create_with_a_name_that_already_exists_throws_DuplicateNameException()
    {
        _factory.ProjectRepository
            .ExistsByNameAsync("Website Redesign", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateProjectRequest { Name = "Website Redesign" };

        await Should.ThrowAsync<DuplicateNameException>(
            () => _factory.ProjectService.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_a_unique_name_persists_and_returns_the_mapped_response()
    {
        _factory.ProjectRepository
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateProjectRequest { Name = "Website Redesign", Description = "Q3 revamp" };

        var response = await _factory.ProjectService.CreateAsync(request, CancellationToken.None);

        response.Name.ShouldBe("Website Redesign");
        response.Description.ShouldBe("Q3 revamp");
        await _factory.ProjectRepository.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _factory.ProjectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_for_a_missing_project_throws_NotFoundException()
    {
        _factory.ProjectRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _factory.ProjectService.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_removes_the_project_and_saves()
    {
        var project = new Project { Id = Guid.NewGuid(), Name = "To Delete" };
        // Delete loads the aggregate (project + tasks) for cascade soft-delete.
        _factory.ProjectRepository
            .GetByIdWithTasksAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _factory.ProjectService.DeleteAsync(project.Id, CancellationToken.None);

        _factory.ProjectRepository.Received(1).Remove(project);
        await _factory.ProjectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
