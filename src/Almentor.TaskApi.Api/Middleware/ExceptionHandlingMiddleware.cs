using System.Net;
using System.Text.Json;
using Almentor.TaskApi.Application.Common.Errors;
using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Models;
using FluentValidation;

namespace Almentor.TaskApi.Api.Middleware;

// Single catch-all for the whole request pipeline. Every exception, expected
// or not, is translated here into the same ApiResponse error envelope with an
// appropriate status code — so no endpoint can leak a raw 500 or an
// inconsistent error shape for bad input.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, error) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorDetail
                {
                    Code = ErrorCodes.ValidationError,
                    Message = "One or more validation errors occurred.",
                    Details = validationEx.Errors
                        .Select(e => new FieldError { Field = e.PropertyName, Message = e.ErrorMessage })
                        .ToList()
                }),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorDetail { Code = ErrorCodes.NotFound, Message = notFoundEx.Message }),

            DuplicateNameException duplicateEx => (
                HttpStatusCode.Conflict,
                new ErrorDetail { Code = ErrorCodes.DuplicateName, Message = duplicateEx.Message }),

            EmailAlreadyExistsException emailEx => (
                HttpStatusCode.Conflict,
                new ErrorDetail { Code = ErrorCodes.EmailTaken, Message = emailEx.Message }),

            InvalidCredentialsException credEx => (
                HttpStatusCode.Unauthorized,
                new ErrorDetail { Code = ErrorCodes.InvalidCredentials, Message = credEx.Message }),

            _ => HandleUnexpected(exception, traceId)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(error, new ResponseMeta { TraceId = traceId });
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private (HttpStatusCode, ErrorDetail) HandleUnexpected(Exception exception, string traceId)
    {
        // Only unanticipated failures are logged as errors here — the expected
        // exceptions above (validation/not-found/duplicate) are normal control
        // flow, not incidents, so they don't pollute error-level logs.
        _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        return (HttpStatusCode.InternalServerError, new ErrorDetail
        {
            Code = ErrorCodes.InternalError,
            // Never leak exception details to the client — log has the real message.
            Message = "An unexpected error occurred. Please try again later."
        });
    }
}
