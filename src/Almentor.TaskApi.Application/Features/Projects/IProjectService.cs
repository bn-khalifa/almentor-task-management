using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;

namespace Almentor.TaskApi.Application.Features.Projects;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct);

    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<ProjectResponse>> GetPagedAsync(PaginationParams pagination, CancellationToken ct);

    Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);
}
