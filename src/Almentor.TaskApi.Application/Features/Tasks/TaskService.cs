using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Common.Parsing;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Domain.Entities;
using Almentor.TaskApi.Domain.Enums;
using FluentValidation;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Almentor.TaskApi.Application.Features.Tasks;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<TaskQueryParameters> _queryValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskService> _logger;
    private readonly ICurrentUserService _currentUser;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskQueryParameters> queryValidator,
        IMapper mapper,
        ILogger<TaskService> logger,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // Must own the parent project. A project owned by someone else is treated
        // as not found — same as if it didn't exist.
        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        if (project is null || project.OwnerId != _currentUser.UserId)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var task = _mapper.Map<TaskItem>(request);
        task.ProjectId = projectId;

        await _taskRepository.AddAsync(task, ct);
        await _taskRepository.SaveChangesAsync(ct);

        task.Project = project;

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<PagedResult<TaskResponse>> GetPagedAsync(
        Guid? projectId, TaskQueryParameters query, CancellationToken ct)
    {
        await _queryValidator.ValidateAndThrowAsync(query, ct);

        // Scoped list: a request for a non-existent OR unowned project's tasks
        // is a 404 (the project isn't visible to this caller), not an empty page.
        if (projectId is not null)
        {
            var project = await _projectRepository.GetByIdAsync(projectId.Value, ct);
            if (project is null || project.OwnerId != _currentUser.UserId)
            {
                throw new NotFoundException(nameof(Project), projectId.Value);
            }
        }

        var typedQuery = ToTypedQuery(_currentUser.UserId, projectId, query);
        var page = await _taskRepository.GetPagedAsync(typedQuery, ct);

        return new PagedResult<TaskResponse>
        {
            Items = _mapper.Map<List<TaskResponse>>(page.Items),
            Total = page.Total,
            Offset = page.Offset,
            Limit = page.Limit
        };
    }

    // Parses the validated raw query into fully-typed values. Safe to parse
    // without re-checking. Defaults: sort=created_at, direction=desc.
    private static TaskListQuery ToTypedQuery(Guid ownerId, Guid? projectId, TaskQueryParameters query) => new()
    {
        OwnerId = ownerId,
        ProjectId = projectId,
        Status = EnumSnakeParser.ParseOrNull<TaskItemStatus>(query.Status),
        Priority = EnumSnakeParser.ParseOrNull<TaskItemPriority>(query.Priority),
        DueDateFrom = query.DueDateFrom,
        DueDateTo = query.DueDateTo,
        Search = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim(),
        Sort = EnumSnakeParser.ParseOrNull<TaskSortField>(query.Sort) ?? TaskSortField.CreatedAt,
        Direction = EnumSnakeParser.ParseOrNull<SortDirection>(query.Direction) ?? SortDirection.Desc,
        Pagination = new PaginationParams { Offset = query.Offset, Limit = query.Limit }
    };

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var task = await GetOwnedOrThrowAsync(id, ct);
        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await GetOwnedOrThrowAsync(id, ct);

        var previousStatus = task.Status;

        _mapper.Map(request, task);

        if (previousStatus == TaskItemStatus.Done && task.Status == TaskItemStatus.Todo)
        {
            _logger.LogWarning(
                "Task {TaskId} status moved from Done back to Todo — unusual transition, allowed.", id);
        }

        _taskRepository.Update(task);
        await _taskRepository.SaveChangesAsync(ct);

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await GetOwnedOrThrowAsync(id, ct);

        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync(ct);
    }

    // A task belongs to the caller iff its project does. GetByIdAsync Includes
    // Project, so Project.OwnerId is available. Unowned/missing → 404.
    private async Task<TaskItem> GetOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task is null || task.Project.OwnerId != _currentUser.UserId)
        {
            throw new NotFoundException("Task", id);
        }

        return task;
    }
}
