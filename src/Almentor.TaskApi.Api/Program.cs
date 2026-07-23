using Almentor.TaskApi.Api.Middleware;
using Almentor.TaskApi.Application;
using Almentor.TaskApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Application layer: Mapster, FluentValidation, use-case services (see AddApplication).
builder.Services.AddApplication();
// Infrastructure layer: EF Core DbContext + SQL Server (see AddInfrastructure).
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// First in the pipeline so it catches exceptions from every later stage.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
