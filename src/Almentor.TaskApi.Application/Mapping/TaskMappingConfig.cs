using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Domain.Entities;
using Almentor.TaskApi.Domain.Enums;
using Mapster;

namespace Almentor.TaskApi.Application.Mapping;

public class TaskMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Precondition: Project must be loaded (ITaskRepository.GetByIdAsync
        // Include()s it) — this mapping is only ever invoked on tasks fetched that way.
        config.NewConfig<TaskItem, TaskResponse>()
            .Map(dest => dest.ProjectName, src => src.Project.Name);

        config.NewConfig<CreateTaskRequest, TaskItem>()
            // A null Status/Priority means "use the default" — applying it here
            // keeps the default co-located with the field it defaults, instead
            // of scattering ?? checks across the service.
            .Map(dest => dest.Status, src => src.Status ?? TaskItemStatus.Todo)
            .Map(dest => dest.Priority, src => src.Priority ?? TaskItemPriority.Medium)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ProjectId)
            .Ignore(dest => dest.Project)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);

        config.NewConfig<UpdateTaskRequest, TaskItem>()
            // .Value is safe here only because UpdateTaskRequestValidator's
            // NotNull rules already ran (via ValidateAndThrowAsync) before this
            // mapping executes — Status/Priority are guaranteed non-null.
            .Map(dest => dest.Status, src => src.Status!.Value)
            .Map(dest => dest.Priority, src => src.Priority!.Value)
            .Ignore(dest => dest.Id)
            // Project is fixed at creation — PUT never reassigns it.
            .Ignore(dest => dest.ProjectId)
            .Ignore(dest => dest.Project)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt);
    }
}
