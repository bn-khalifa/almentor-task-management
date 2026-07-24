using System.Text.Json;
using System.Text.Json.Serialization;
using Almentor.TaskApi.Api.Extensions;
using Almentor.TaskApi.Api.Middleware;
using Almentor.TaskApi.Api.Services;
using Almentor.TaskApi.Application;
using Almentor.TaskApi.Application.Common.Errors;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Infrastructure;
using Almentor.TaskApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Wire format for Status/Priority: todo/in_progress/done, low/medium/high.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));

// Model-binding failures (malformed JSON, an unrecognized enum string, etc.)
// happen before a controller action — and therefore before our own
// FluentValidation call — ever runs. Without this, ASP.NET Core's default
// ProblemDetails shape would leak through, breaking the "every response uses
// the same envelope" guarantee and the "no inconsistent error shape" rule.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var entries = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToList();

        // A bad JSON value (e.g. an unrecognized enum string) produces both a
        // precise "$.status"-keyed error and a generic "the request field is
        // required" fallback for the whole parameter, since binding failed
        // outright. Once a path-specific error exists, the generic one is pure
        // noise — drop it so the client sees one clear message, not two.
        var hasFieldSpecificError = entries.Any(entry => entry.Key.Contains('.'));

        var details = entries
            .Where(entry => !hasFieldSpecificError || entry.Key.Contains('.'))
            .SelectMany(entry => entry.Value!.Errors.Select(error => new FieldError
            {
                Field = CleanFieldName(entry.Key),
                Message = CleanErrorMessage(error.ErrorMessage)
            }))
            .ToList();

        var body = ApiResponse.Fail(
            new ErrorDetail
            {
                Code = ErrorCodes.ValidationError,
                Message = "One or more validation errors occurred.",
                Details = details
            },
            new ResponseMeta { TraceId = context.HttpContext.TraceIdentifier });

        return new BadRequestObjectResult(body);
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Application layer: Mapster, FluentValidation, use-case services (see AddApplication).
builder.Services.AddApplication();
// Infrastructure layer: EF Core DbContext + SQL Server + auth services (see AddInfrastructure).
builder.Services.AddInfrastructure(builder.Configuration);

// Auth: JWT bearer + the current-user accessor that reads the token's `sub`.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// Apply pending migrations, then seed sample data if the DB is empty — so a
// bare `docker compose up` (or a fresh `dotnet run`) yields a fully working,
// browsable API with zero manual setup. Seeding is skipped under the
// WebApplicationFactory-driven integration tests (env "Testing"), which manage
// their own known dataset per test via SqlServerFixture/IntegrationTestBase.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync(seed: !app.Environment.IsEnvironment("Testing"));
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar
}

// First in the pipeline so it catches exceptions from every later stage.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// ModelState keys for a JSON path error look like "$.status" — strip the
// System.Text.Json path prefix so the client sees the plain field name.
static string CleanFieldName(string key) => key.StartsWith("$.") ? key[2..] : key;

// System.Text.Json's raw conversion-failure message leaks CLR type names
// (e.g. "System.Nullable`1[...TaskItemStatus]") — replace it with a message
// that's actually meaningful to an API client, per field name is enough context.
static string CleanErrorMessage(string rawMessage) =>
    rawMessage.Contains("could not be converted")
        ? "The value provided is not valid for this field."
        : rawMessage;

// Makes the implicit Program class reachable so integration tests can spin up
// the real app via WebApplicationFactory<Program> (top-level statements alone
// generate an internal Program class, invisible outside this assembly).
public partial class Program;
