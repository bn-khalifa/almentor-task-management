using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

/// <summary>Issues a signed JWT for an authenticated user. Implemented in Infrastructure.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Returns the signed token string and the UTC instant it expires.</summary>
    (string Token, DateTime ExpiresAtUtc) Generate(User user);
}
