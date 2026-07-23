using System.Reflection;
using Almentor.TaskApi.Application.Features.Projects;
using Almentor.TaskApi.Application.Features.Tasks;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Almentor.TaskApi.Application;

/// <summary>
/// Composition root for the Application layer: Mapster mapping config,
/// FluentValidation validators (both discovered by assembly scan, so a new
/// feature's IRegister/AbstractValidator is picked up automatically), and
/// the use-case services themselves.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var mapperConfig = new TypeAdapterConfig();
        mapperConfig.Scan(assembly);
        services.AddSingleton(mapperConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();

        return services;
    }
}
