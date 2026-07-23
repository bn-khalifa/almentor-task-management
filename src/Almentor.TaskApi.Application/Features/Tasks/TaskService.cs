using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
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
    private readonly IMapper _mapper;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IMapper mapper,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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
