using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Domain.Entities;
using FluentValidation;
using MapsterMapper;

namespace Almentor.TaskApi.Application.Features.Projects;

/// Orchestrates Project use-cases: validate, enforce business rules (duplicate
/// name, existence) and per-user ownership, then delegate persistence.
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public ProjectService(
        IProjectRepository repository,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var ownerId = _currentUser.UserId;

        // Name uniqueness is per-owner: the pre-check and the DB's composite
        // filtered unique index both scope by owner.
        if (await _repository.ExistsByNameAsync(ownerId, request.Name, excludeId: null, ct))
        {
            throw new DuplicateNameException(request.Name);
        }

        var project = _mapper.Map<Project>(request);
        project.OwnerId = ownerId;

        await _repository.AddAsync(project, ct);
        await _repository.SaveChangesAsync(ct);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var project = await GetOwnedOrThrowAsync(id, ct);
        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task<PagedResult<ProjectResponse>> GetPagedAsync(PaginationParams pagination, CancellationToken ct)
    {
        var page = await _repository.GetPagedAsync(_currentUser.UserId, pagination, ct);

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

        var project = await GetOwnedOrThrowAsync(id, ct);

        if (await _repository.ExistsByNameAsync(_currentUser.UserId, request.Name, excludeId: id, ct))
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
        // Load tracked WITH tasks so the delete cascades to them in memory —
        // required for soft-delete, since the DB's ON DELETE CASCADE only fires
        // on a hard delete, which the DbContext converts away.
        var project = await _repository.GetByIdWithTasksAsync(id, ct);
        EnsureOwned(project, id);

        _repository.Remove(project!);
        await _repository.SaveChangesAsync(ct);
    }

    private async Task<Project> GetOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(id, ct);
        EnsureOwned(project, id);
        return project!;
    }

    // A project the caller doesn't own is treated as not found — never reveal
    // that someone else's project exists (404, not 403).
    private void EnsureOwned(Project? project, Guid id)
    {
        if (project is null || project.OwnerId != _currentUser.UserId)
        {
            throw new NotFoundException(nameof(Project), id);
        }
    }
}
