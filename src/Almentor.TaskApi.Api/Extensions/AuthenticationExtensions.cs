using System.Text;
using System.Text.Json;
using Almentor.TaskApi.Application.Common.Errors;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Almentor.TaskApi.Api.Extensions;

/// <summary>
/// Configures JWT bearer authentication and makes its 401/403 responses use the
/// same <see cref="ApiResponse"/> envelope as everything else, so clients never
/// see an inconsistent error shape.
/// </summary>
public static class AuthenticationExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the raw JWT claim names (so `sub` stays `sub`, not remapped
                // to the long ClaimTypes.NameIdentifier URI) — CurrentUserService reads `sub`.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        // Suppress the default empty 401 and write our envelope instead.
                        ctx.HandleResponse();
                        await WriteEnvelopeAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized,
                            ErrorCodes.Unauthorized, "Authentication is required to access this resource.");
                    },
                    OnForbidden = ctx =>
                        WriteEnvelopeAsync(ctx.HttpContext, StatusCodes.Status403Forbidden,
                            ErrorCodes.Forbidden, "You do not have permission to access this resource.")
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static Task WriteEnvelopeAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = ApiResponse.Fail(
            new ErrorDetail { Code = code, Message = message },
            new ResponseMeta { TraceId = context.TraceIdentifier });

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
