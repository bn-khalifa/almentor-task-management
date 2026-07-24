namespace Almentor.TaskApi.Application.Common.Interfaces;

/// <summary>
/// Exposes the authenticated caller's identity to the Application layer, sourced
/// from the JWT by the API. Services use <see cref="UserId"/> to scope every
/// project/task operation to the current owner. Implemented in the API layer,
/// which has access to the HTTP context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's id. Throws if accessed on an unauthenticated request (a bug — endpoints are [Authorize]d).</summary>
    Guid UserId { get; }
}
