using Almentor.TaskApi.Application.Features.Projects.Dtos;
using Almentor.TaskApi.Domain.Entities;
using Mapster;

namespace Almentor.TaskApi.Application.Mapping;

// Mapster mapping rules for Project.
public class ProjectMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectResponse>();

        config.NewConfig<CreateProjectRequest, Project>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Tasks);

        // Applied onto an existing tracked entity in the service (adapter.Adapt(existingProject)),
        // so identity/audit/navigation fields are simply not part of this map.
        config.NewConfig<UpdateProjectRequest, Project>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Tasks);
    }
}
