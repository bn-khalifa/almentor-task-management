namespace Almentor.TaskApi.Application.Features.Auth.Dtos;

/// <summary>What register/login return: the bearer token, when it expires, and who it's for.</summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
