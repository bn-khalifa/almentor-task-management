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

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskQueryParameters> queryValidator,
        IMapper mapper,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException(nameof(Project), projectId);

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

        // Scoped list: a request for a non-existent project's tasks is a 404,
        // not an empty page as the project itself doesn't exist.
        if (projectId is not null && await _projectRepository.GetByIdAsync(projectId.Value, ct) is null)
        {
            throw new NotFoundException(nameof(Project), projectId.Value);
        }

        var typedQuery = ToTypedQuery(projectId, query);
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
    private static TaskListQuery ToTypedQuery(Guid? projectId, TaskQueryParameters query) => new()
    {
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
        var task = await _taskRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Task", id);

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await _taskRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Task", id);

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
        var task = await _taskRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Task", id);

        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync(ct);
    }
}
