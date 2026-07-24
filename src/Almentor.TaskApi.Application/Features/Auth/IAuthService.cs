using Almentor.TaskApi.Application.Features.Auth.Dtos;

namespace Almentor.TaskApi.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}
