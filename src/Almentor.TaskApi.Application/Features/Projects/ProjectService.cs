using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Domain.Entities;
using FluentValidation;
using MapsterMapper;

namespace Almentor.TaskApi.Application.Features.Projects;

/// Orchestrates Project use-cases: validate the request, enforce business rules
/// (duplicate name, existence), then delegate persistence to the repository.
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;
    private readonly IMapper _mapper;

    public ProjectService(
        IProjectRepository repository,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator,
        IMapper mapper)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // App-layer pre-check gives a clean 409 in the common case; the unique
        // index in the DB is the race-safe backstop the repository translates
        // into the same DuplicateNameException on SaveChangesAsync.
        if (await _repository.ExistsByNameAsync(request.Name, excludeId: null, ct))
        {
            throw new DuplicateNameException(request.Name);
        }

        var project = _mapper.Map<Project>(request);

        await _repository.AddAsync(project, ct);
        await _repository.SaveChangesAsync(ct);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Project), id);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task<PagedResult<ProjectResponse>> GetPagedAsync(PaginationParams pagination, CancellationToken ct)
    {
        var page = await _repository.GetPagedAsync(pagination, ct);

        return new PagedResult<ProjectResponse>
        {
            Items = _mapper.Map<List<ProjectResponse>>(page.Items),
            Total = page.Total,
            Offset = page.Offset,
            Limit = page.Limit
        };
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var project = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Project), id);

        if (await _repository.ExistsByNameAsync(request.Name, excludeId: id, ct))
        {
            throw new DuplicateNameException(request.Name);
        }

        _mapper.Map(request, project);
        _repository.Update(project);
        await _repository.SaveChangesAsync(ct);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Project), id);

        // Cascade delete of tasks is handled in configurations
        _repository.Remove(project);
        await _repository.SaveChangesAsync(ct);
    }
}
