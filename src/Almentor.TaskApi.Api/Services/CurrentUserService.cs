using System.IdentityModel.Tokens.Jwt;
using Almentor.TaskApi.Application.Common.Interfaces;

namespace Almentor.TaskApi.Api.Services;

/// <summary>
/// Reads the authenticated user's id from the JWT's `sub` claim on the current
/// request. Lives in the API layer because it's the only layer that knows about
/// HttpContext; the Application layer sees just <see cref="ICurrentUserService"/>.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            // `sub` is preserved (not remapped) because JwtBearer is configured
            // with MapInboundClaims = false in Program.cs.
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            // Reaching here without a valid id means an [Authorize]d endpoint let
            // an unauthenticated/malformed request through — a configuration bug.
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("No authenticated user id on the current request.");
        }
    }
}
